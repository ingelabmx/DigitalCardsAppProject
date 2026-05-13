using DigitalCards.Application;
using DigitalCards.Infrastructure;
using DigitalCards.Web;
using DigitalCards.Web.Operations;
using DigitalCards.Web.Pilot;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authentication.Cookies;

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
builder.Services.AddScoped<PilotAccessService>();
builder.Services.AddDigitalCardsOperations(builder.Configuration);
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
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
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
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
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

var app = builder.Build();

app.UseDigitalCardsForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

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
