using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace DigitalCards.Web.Security;

public static class DigitalCardsSecurityExtensions
{
    public static IServiceCollection AddDigitalCardsSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var security = configuration
            .GetSection(DigitalCardsSecurityOptions.SectionName)
            .Get<DigitalCardsSecurityOptions>() ?? new DigitalCardsSecurityOptions();

        services.Configure<DigitalCardsSecurityOptions>(
            configuration.GetSection(DigitalCardsSecurityOptions.SectionName));
        services.AddSingleton<DigitalCardsPathRateLimitStore>();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var classification = ClassifyRequest(context.Request.Path);
                return classification switch
                {
                    SecurityRateLimitPolicyNames.Auth => CreateFixedWindowPartition(
                        context,
                        classification,
                        security.RateLimiting.AuthPermitLimit,
                        security.RateLimiting.AuthWindowSeconds),
                    SecurityRateLimitPolicyNames.PublicWrite => CreateFixedWindowPartition(
                        context,
                        classification,
                        security.RateLimiting.PublicWritePermitLimit,
                        security.RateLimiting.PublicWriteWindowSeconds),
                    SecurityRateLimitPolicyNames.WalletPublic => CreateFixedWindowPartition(
                        context,
                        classification,
                        security.RateLimiting.WalletPermitLimit,
                        security.RateLimiting.WalletWindowSeconds),
                    _ => RateLimitPartition.GetNoLimiter("digitalcards-unlimited")
                };
            });
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await context.HttpContext.Response.WriteAsync(
                    "Too many requests.",
                    cancellationToken);
            };

            AddFixedWindowPolicy(
                options,
                SecurityRateLimitPolicyNames.Auth,
                security.RateLimiting.AuthPermitLimit,
                security.RateLimiting.AuthWindowSeconds);
            AddFixedWindowPolicy(
                options,
                SecurityRateLimitPolicyNames.PublicWrite,
                security.RateLimiting.PublicWritePermitLimit,
                security.RateLimiting.PublicWriteWindowSeconds);
            AddFixedWindowPolicy(
                options,
                SecurityRateLimitPolicyNames.WalletPublic,
                security.RateLimiting.WalletPermitLimit,
                security.RateLimiting.WalletWindowSeconds);
        });

        return services;
    }

    public static IApplicationBuilder UseDigitalCardsSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers.TryAdd("X-Content-Type-Options", "nosniff");
                headers.TryAdd("X-Frame-Options", "DENY");
                headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");

                if (ShouldDisableCache(context.Request.Path))
                {
                    headers.CacheControl = "no-store, no-cache, must-revalidate";
                    headers.Pragma = "no-cache";
                    headers.Expires = "0";
                }

                return Task.CompletedTask;
            });

            await next();
        });
    }

    public static IApplicationBuilder UseDigitalCardsPathRateLimits(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var classification = ClassifyRequest(context.Request.Path);
            if (classification is null)
            {
                await next();
                return;
            }

            var security = context.RequestServices
                .GetRequiredService<IOptions<DigitalCardsSecurityOptions>>()
                .Value;
            var (permitLimit, windowSeconds) = classification switch
            {
                SecurityRateLimitPolicyNames.Auth => (
                    security.RateLimiting.AuthPermitLimit,
                    security.RateLimiting.AuthWindowSeconds),
                SecurityRateLimitPolicyNames.PublicWrite => (
                    security.RateLimiting.PublicWritePermitLimit,
                    security.RateLimiting.PublicWriteWindowSeconds),
                SecurityRateLimitPolicyNames.WalletPublic => (
                    security.RateLimiting.WalletPermitLimit,
                    security.RateLimiting.WalletWindowSeconds),
                _ => (int.MaxValue, 60)
            };

            var store = context.RequestServices.GetRequiredService<DigitalCardsPathRateLimitStore>();
            if (!store.TryAcquire(context, classification, permitLimit, windowSeconds, out var retryAfterSeconds))
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
                await context.Response.WriteAsync("Too many requests.", context.RequestAborted);
                return;
            }

            await next();
        });
    }

    private static void AddFixedWindowPolicy(
        RateLimiterOptions options,
        string policyName,
        int permitLimit,
        int windowSeconds)
    {
        options.AddPolicy(policyName, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                GetPartitionKey(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = Math.Max(1, permitLimit),
                    Window = TimeSpan.FromSeconds(Math.Max(1, windowSeconds)),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
    }

    private static RateLimitPartition<string> CreateFixedWindowPartition(
        HttpContext context,
        string classification,
        int permitLimit,
        int windowSeconds)
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            $"{classification}:{GetPartitionKey(context)}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(1, permitLimit),
                Window = TimeSpan.FromSeconds(Math.Max(1, windowSeconds)),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    }

    private static string GetPartitionKey(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString()
            ?? context.Request.Headers.Host.ToString()
            ?? "unknown";
    }

    private static string? ClassifyRequest(PathString path)
    {
        if (path.StartsWithSegments("/Admin/Login", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/Business/Login", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/Client/Login", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/Business/ForgotPassword", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/Business/ResetPassword", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/Client/ForgotPassword", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/Client/ResetPassword", StringComparison.OrdinalIgnoreCase))
        {
            return SecurityRateLimitPolicyNames.Auth;
        }

        if (path.StartsWithSegments("/Register", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/Enroll", StringComparison.OrdinalIgnoreCase))
        {
            return SecurityRateLimitPolicyNames.PublicWrite;
        }

        if (path.StartsWithSegments("/Wallet", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/apple-wallet", StringComparison.OrdinalIgnoreCase))
        {
            return SecurityRateLimitPolicyNames.WalletPublic;
        }

        return null;
    }

    private static bool ShouldDisableCache(PathString path)
    {
        return path.StartsWithSegments("/Admin", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/Business", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/Client", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/Dev", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record RateLimitWindow(DateTimeOffset StartedAt, int Count);

    private sealed class DigitalCardsPathRateLimitStore
    {
        private readonly Dictionary<string, Dictionary<string, RateLimitWindow>> _windows = [];
        private readonly object _lock = new();

        public bool TryAcquire(
            HttpContext context,
            string classification,
            int permitLimit,
            int windowSeconds,
            out int retryAfterSeconds)
        {
            var now = DateTimeOffset.UtcNow;
            var safeWindowSeconds = Math.Max(1, windowSeconds);
            var key = $"{classification}:{GetPartitionKey(context)}";

            lock (_lock)
            {
                if (!_windows.TryGetValue(classification, out var policyWindows))
                {
                    policyWindows = [];
                    _windows[classification] = policyWindows;
                }

                if (!policyWindows.TryGetValue(key, out var window) ||
                    now >= window.StartedAt.AddSeconds(safeWindowSeconds))
                {
                    policyWindows[key] = new RateLimitWindow(now, Count: 1);
                    retryAfterSeconds = safeWindowSeconds;
                    return true;
                }

                if (window.Count < Math.Max(1, permitLimit))
                {
                    policyWindows[key] = window with { Count = window.Count + 1 };
                    retryAfterSeconds = safeWindowSeconds;
                    return true;
                }

                retryAfterSeconds = Math.Max(
                    1,
                    (int)Math.Ceiling((window.StartedAt.AddSeconds(safeWindowSeconds) - now).TotalSeconds));
                return false;
            }
        }
    }
}
