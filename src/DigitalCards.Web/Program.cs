using DigitalCards.Application;
using DigitalCards.Infrastructure;
using DigitalCards.Infrastructure.Branding;
using DigitalCards.Web;
using DigitalCards.Web.Branding;
using DigitalCards.Web.Landing;
using DigitalCards.Web.Operations;
using DigitalCards.Web.Pilot;
using DigitalCards.Web.Security;
using DigitalCards.Web.Workers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.Local.json", optional: true, reloadOnChange: true);

var skipUserLocalConfiguration = string.Equals(
    Environment.GetEnvironmentVariable("DigitalCards__SkipUserLocalConfiguration"),
    "true",
    StringComparison.OrdinalIgnoreCase);

if (!skipUserLocalConfiguration)
{
    var userLocalConfigurationPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".digitalcards",
        "appsettings.Local.json");

    builder.Configuration.AddJsonFile(userLocalConfigurationPath, optional: true, reloadOnChange: true);
}

builder.Configuration
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Services.Configure<PilotOptions>(builder.Configuration.GetSection(PilotOptions.SectionName));
builder.Services.Configure<LandingOptions>(builder.Configuration.GetSection(LandingOptions.SectionName));
builder.Services.AddScoped<PilotAccessService>();
builder.Services.AddScoped<BusinessLogoUploadService>();
builder.Services.AddDigitalCardsOperations(builder.Configuration);
builder.Services.AddDigitalCardsSecurity(builder.Configuration);
builder.Services.AddRazorPages();
builder.Services
    .AddAuthentication(BusinessAuth.Scheme)
    .AddCookie(BusinessAuth.Scheme, options =>
    {
        options.Cookie.Name = ".DigitalCards.Business";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/Business/Login";
        options.AccessDeniedPath = "/Business/Login";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
    })
    .AddCookie(AdminAuth.Scheme, options =>
    {
        options.Cookie.Name = ".DigitalCards.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/Admin/Login";
        options.AccessDeniedPath = "/Admin/Login";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    })
    .AddCookie(ClientAuth.Scheme, options =>
    {
        options.Cookie.Name = ".DigitalCards.Client";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/Client/Login";
        options.AccessDeniedPath = "/Client/Login";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(BusinessAuth.Policy, policy =>
    {
        policy.AuthenticationSchemes.Add(BusinessAuth.Scheme);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(BusinessAuth.RoleClaim, BusinessAuth.Role);
        policy.RequireClaim(BusinessAuth.BusinessIdClaim);
    });
    options.AddPolicy(AdminAuth.Policy, policy =>
    {
        policy.AuthenticationSchemes.Add(AdminAuth.Scheme);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(AdminAuth.RoleClaim, AdminAuth.Role);
        policy.RequireClaim(AdminAuth.AdminUserIdClaim);
    });
    options.AddPolicy(ClientAuth.Policy, policy =>
    {
        policy.AuthenticationSchemes.Add(ClientAuth.Scheme);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(ClientAuth.RoleClaim, ClientAuth.Role);
        policy.RequireClaim(ClientAuth.ClientIdClaim);
    });
});
builder.Services.AddDigitalCardsApplication();
builder.Services.AddDigitalCardsInfrastructure(builder.Configuration);
builder.Services.AddHostedService<SubscriptionExpiryWorker>();

var app = builder.Build();

app.UseDigitalCardsForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseDigitalCardsSecurityHeaders();
app.Use(async (context, next) =>
{
    var landing = context.RequestServices.GetRequiredService<IOptions<LandingOptions>>().Value;
    if (LandingHost.ShouldRedirectToApp(context, landing))
    {
        var appUrl = landing.AppUrl.TrimEnd('/');
        var destination = $"{appUrl}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
        context.Response.Redirect(destination, permanent: false);
        return;
    }

    await next();
});
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // 1 año para assets con fingerprint (?v=...), 7 días para los demás
        var hasVersion = ctx.Context.Request.Query.ContainsKey("v");
        var maxAge = hasVersion ? 31536000 : 604800;
        ctx.Context.Response.Headers.Append(
            "Cache-Control", $"public, max-age={maxAge}{(hasVersion ? ", immutable" : "")}");
    }
});

var logoUploadOptions = app.Services.GetRequiredService<IOptions<BusinessLogoUploadOptions>>().Value;
var logoUploadRoot = logoUploadOptions.GetPhysicalRoot();
Directory.CreateDirectory(logoUploadRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(logoUploadRoot),
    RequestPath = logoUploadOptions.GetRequestPath()
});

app.UseRouting();

app.UseDigitalCardsPathRateLimits();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapDigitalCardsHealthChecks();
app.MapRazorPages();
app.MapAppleWalletPassDownloads();
app.MapAppleWalletWebService();
app.MapWalletDiagnostics();

app.Run();

public partial class Program
{
}
