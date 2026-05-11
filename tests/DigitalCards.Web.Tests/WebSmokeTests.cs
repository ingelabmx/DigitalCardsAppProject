using DigitalCards.Application;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Contains("DigitalCards migration shell", html);
        Assert.Contains("Registro cliente", html);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsSuccess()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AppleWalletPage_WithValidToken_ReturnsPendingState()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient();
        var token = await CreateEnrollmentTokenAsync(fake.Factory, "webapple1");

        var html = await client.GetStringAsync($"/Wallet/Apple/{token}");

        Assert.Contains("Apple Wallet pendiente", html);
        Assert.Contains("apple-wallet-pending", html);
    }

    [Fact]
    public async Task AppleWalletPage_WithInvalidToken_ReturnsNotFoundState()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient();

        var html = await client.GetStringAsync("/Wallet/Apple/missing-token");

        Assert.Contains("Link no valido", html);
        Assert.Contains("apple-wallet-not-found", html);
    }

    private FakeIntegrationFactory WithFakeIntegrations()
    {
        return new FakeIntegrationFactory(_factory);
    }

    private sealed class FakeIntegrationFactory : IDisposable
    {
        private static readonly IReadOnlyDictionary<string, string?> Overrides = new Dictionary<string, string?>
        {
            ["DigitalCards__UseFakeIntegrations"] = "true",
            ["DigitalCards__PersistenceProvider"] = "InMemory",
            ["DigitalCards__GoogleWallet__Provider"] = "Fake",
            ["DigitalCards__AppleWallet__Provider"] = "Fake",
            ["DigitalCards__Email__Provider"] = "Fake",
            ["DigitalCards__PublicBaseUrl"] = string.Empty
        };

        private readonly Dictionary<string, string?> _previousValues;

        public FakeIntegrationFactory(WebApplicationFactory<Program> baseFactory)
        {
            _previousValues = Overrides.ToDictionary(
                pair => pair.Key,
                pair => Environment.GetEnvironmentVariable(pair.Key));

            foreach (var pair in Overrides)
            {
                Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }

            Factory = baseFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["DigitalCards:UseFakeIntegrations"] = "true",
                        ["DigitalCards:PersistenceProvider"] = "InMemory",
                        ["DigitalCards:GoogleWallet:Provider"] = "Fake",
                        ["DigitalCards:AppleWallet:Provider"] = "Fake",
                        ["DigitalCards:Email:Provider"] = "Fake",
                        ["DigitalCards:PublicBaseUrl"] = string.Empty
                    });
                });
            });
        }

        public WebApplicationFactory<Program> Factory { get; }

        public void Dispose()
        {
            Factory.Dispose();

            foreach (var pair in _previousValues)
            {
                Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }
        }
    }

    private static async Task<string> CreateEnrollmentTokenAsync(
        WebApplicationFactory<Program> factory,
        string userName)
    {
        using var scope = factory.Services.CreateScope();
        var app = scope.ServiceProvider.GetRequiredService<DigitalCardsAppService>();

        var client = await app.RegisterClientAsync(new RegisterClientCommand(
            userName,
            "Web",
            "Apple",
            $"{userName}@example.test"));

        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));

        var enrollment = await app.EnrollClientAsync(new EnrollClientCommand(
            business!.Id,
            client.UserName,
            "http://localhost"));

        return enrollment.Card.EnrollmentToken;
    }
}
