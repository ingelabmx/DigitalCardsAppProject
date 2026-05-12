using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DigitalCards.Web.Operations;

public static class DigitalCardsOperationsExtensions
{
    public static IServiceCollection AddDigitalCardsOperations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ProductionOperationsOptions>(
            configuration.GetSection(ProductionOperationsOptions.SectionName));

        var options = configuration
            .GetSection(ProductionOperationsOptions.SectionName)
            .Get<ProductionOperationsOptions>() ?? new ProductionOperationsOptions();

        services.Configure<ForwardedHeadersOptions>(forwarded =>
        {
            forwarded.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto |
                ForwardedHeaders.XForwardedHost;

            foreach (var proxy in options.KnownProxies)
            {
                if (IPAddress.TryParse(proxy, out var address))
                {
                    forwarded.KnownProxies.Add(address);
                }
            }

            if (options.TrustAllForwardedHeaders)
            {
                forwarded.KnownNetworks.Clear();
                forwarded.KnownProxies.Clear();
            }
        });

        var dataProtection = services
            .AddDataProtection()
            .SetApplicationName("DigitalCardsApp");

        if (!string.IsNullOrWhiteSpace(options.DataProtectionKeysPath))
        {
            Directory.CreateDirectory(options.DataProtectionKeysPath);
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(options.DataProtectionKeysPath));
        }

        services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("Application is running."), tags: ["live"])
            .AddCheck<DigitalCardsConfigurationHealthCheck>("configuration", tags: ["ready"])
            .AddCheck<MySqlReadinessHealthCheck>("mysql", tags: ["ready"]);

        return services;
    }

    public static IApplicationBuilder UseDigitalCardsForwardedHeaders(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<IOptions<ProductionOperationsOptions>>().Value;
        if (options.EnableForwardedHeaders)
        {
            app.UseForwardedHeaders();
        }

        return app;
    }

    public static IEndpointRouteBuilder MapDigitalCardsHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks(
            "/health",
            new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("live"),
                ResponseWriter = SafeHealthResponseWriter.WriteAsync
            });

        endpoints.MapHealthChecks(
            "/health/ready",
            new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("ready"),
                ResponseWriter = SafeHealthResponseWriter.WriteAsync,
                ResultStatusCodes =
                {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status200OK,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
                }
            });

        return endpoints;
    }
}
