using DigitalCards.Application;
using DigitalCards.Infrastructure;
using DigitalCards.Web;

var builder = WebApplication.CreateBuilder(args);

var userLocalConfiguration = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".digitalcards",
    "appsettings.Local.json");

builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.Local.json", optional: true, reloadOnChange: true)
    .AddJsonFile(userLocalConfiguration, optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Services.AddRazorPages();
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

app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapRazorPages();
app.MapAppleWalletPassDownloads();
app.MapAppleWalletWebService();

app.Run();

public partial class Program
{
}
