using DigitalCards.Application;
using DigitalCards.Infrastructure;
using DigitalCards.Web;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

var userLocalConfiguration = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".digitalcards",
    "appsettings.Local.json");

var skipUserLocalConfiguration = string.Equals(
    Environment.GetEnvironmentVariable("DigitalCards__SkipUserLocalConfiguration"),
    "true",
    StringComparison.OrdinalIgnoreCase);

builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.Local.json", optional: true, reloadOnChange: true);

if (!skipUserLocalConfiguration)
{
    builder.Configuration.AddJsonFile(userLocalConfiguration, optional: true, reloadOnChange: true);
}

builder.Configuration
    .AddEnvironmentVariables()
    .AddCommandLine(args);

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
});
builder.Services.AddHealthChecks();
builder.Services.AddDigitalCardsApplication();
builder.Services.AddDigitalCardsInfrastructure(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapRazorPages();
app.MapAppleWalletPassDownloads();
app.MapAppleWalletWebService();
app.MapWalletDiagnostics();

app.Run();

public partial class Program
{
}
