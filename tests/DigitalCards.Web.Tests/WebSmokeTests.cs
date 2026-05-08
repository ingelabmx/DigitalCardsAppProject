using Microsoft.AspNetCore.Mvc.Testing;

namespace DigitalCards.Web.Tests;

public sealed class WebSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WebSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HomePage_ReturnsMigrationShell()
    {
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Contains("DigitalCards migration shell", html);
        Assert.Contains("Registro cliente", html);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsSuccess()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }
}

