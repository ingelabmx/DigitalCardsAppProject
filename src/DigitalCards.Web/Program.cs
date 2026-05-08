using DigitalCards.Application;
using DigitalCards.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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

app.Run();

public partial class Program
{
}
