using System.Net;
using System.Text.RegularExpressions;
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
    public async Task BusinessDashboard_WithoutCookie_RedirectsToLogin()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Business/Dashboard");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Business/Login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task BusinessLogin_WithValidCredentials_EmitsCookieAndRedirectsToDashboard()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await LoginBusinessAsync(client);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Business/Dashboard", response.Headers.Location?.OriginalString);
        Assert.True(HasBusinessCookie(response));
    }

    [Fact]
    public async Task BusinessLogin_WithInvalidCredentials_DoesNotEmitCookie()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await LoginBusinessAsync(client, password: "wrong-password");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Credenciales de negocio invalidas", html);
        Assert.False(HasBusinessCookie(response));
    }

    [Fact]
    public async Task BusinessPages_WithValidCookie_ReturnOk()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginBusinessAsync(client);

        foreach (var path in new[] { "/Business/Dashboard", "/Business/Enroll", "/Business/Stamp" })
        {
            var response = await client.GetAsync(path);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task BusinessEnrollAndStamp_UseAuthenticatedBusiness_WhenBusinessIdIsTampered()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginBusinessAsync(client);
        var userName = NewLegacySafeUserName("wa");
        await RegisterClientAsync(fake.Factory, userName);
        var tamperedBusinessId = "22222222-2222-2222-2222-222222222222";

        var enrollToken = await GetAntiforgeryTokenAsync(client, $"/Business/Enroll?businessId={tamperedBusinessId}");
        var enrollResponse = await client.PostAsync(
            $"/Business/Enroll?businessId={tamperedBusinessId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.UserNameOrEmail"] = userName,
                ["__RequestVerificationToken"] = enrollToken
            }));
        var enrollHtml = await enrollResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, enrollResponse.StatusCode);
        Assert.Contains("Correo generado", enrollHtml);

        var stampToken = await GetAntiforgeryTokenAsync(client, $"/Business/Stamp?businessId={tamperedBusinessId}");
        var stampResponse = await client.PostAsync(
            $"/Business/Stamp?businessId={tamperedBusinessId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.UserNameOrEmail"] = userName,
                ["__RequestVerificationToken"] = stampToken
            }));
        var stampHtml = await stampResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, stampResponse.StatusCode);
        Assert.Contains("Sello agregado", stampHtml);
        Assert.Contains("data-testid=\"current-stamps\">2</strong>", stampHtml);
    }

    [Fact]
    public async Task BusinessLogout_ClearsCookieAndRedirectsToLogin()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginBusinessAsync(client);

        var response = await client.GetAsync("/Business/Logout");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Business/Login", response.Headers.Location?.OriginalString);
        Assert.True(HasExpiredBusinessCookie(response));
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

    [Fact]
    public async Task AppleWalletDownload_WithFakeProvider_DoesNotExposePkpass()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient();
        var token = await CreateEnrollmentTokenAsync(fake.Factory, "webapple2");

        var response = await client.GetAsync($"/Wallet/Apple/Download/{token}");

        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotEqual("application/vnd.apple.pkpass", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AppleWalletPkpassExtensionDownload_WithFakeProvider_DoesNotExposePkpass()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient();
        var token = await CreateEnrollmentTokenAsync(fake.Factory, "webapple3");

        var response = await client.GetAsync($"/Wallet/Apple/Download/{token}.pkpass");

        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotEqual("application/vnd.apple.pkpass", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AppleWalletPkpassExtensionDownload_HeadRequest_IsRouted()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient();
        var token = await CreateEnrollmentTokenAsync(fake.Factory, "webapple4");

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, $"/Wallet/Apple/Download/{token}.pkpass"));

        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task AppleWalletWebServicePassEndpoint_WithoutAuthorization_ReturnsUnauthorized()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient();

        var response = await client.GetAsync("/apple-wallet/v1/passes/pass.com.example.digitalcards/serial-123");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AppleWalletWebServiceRegistrations_WithoutUpdates_ReturnsNoContent()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient();

        var response = await client.GetAsync("/apple-wallet/v1/devices/device-123/registrations/pass.com.example.digitalcards");

        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task WalletDiagnostics_WhenDisabled_ReturnsNotFound()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient();

        var response = await client.GetAsync("/internal/wallet-diagnostics/1");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
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
            ["DigitalCards__PublicBaseUrl"] = string.Empty,
            ["DigitalCards__SkipUserLocalConfiguration"] = "true"
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

    private static async Task RegisterClientAsync(
        WebApplicationFactory<Program> factory,
        string userName)
    {
        using var scope = factory.Services.CreateScope();
        var app = scope.ServiceProvider.GetRequiredService<DigitalCardsAppService>();

        await app.RegisterClientAsync(new RegisterClientCommand(
            userName,
            "Web",
            "Auth",
            $"{userName}@example.test"));
    }

    private static async Task<HttpResponseMessage> LoginBusinessAsync(
        HttpClient client,
        string email = "demo@digitalcards.test",
        string password = "business123")
    {
        var token = await GetAntiforgeryTokenAsync(client, "/Business/Login");
        return await client.PostAsync(
            "/Business/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.Email"] = email,
                ["Input.Password"] = password,
                ["__RequestVerificationToken"] = token
            }));
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        var html = await client.GetStringAsync(path);
        return ExtractAntiforgeryToken(html);
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            "<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"(?<value>[^\"]+)\"[^>]*>",
            RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            match = Regex.Match(
                html,
                "<input[^>]*value=\"(?<value>[^\"]+)\"[^>]*name=\"__RequestVerificationToken\"[^>]*>",
                RegexOptions.IgnoreCase);
        }

        return match.Success
            ? WebUtility.HtmlDecode(match.Groups["value"].Value)
            : throw new InvalidOperationException("Antiforgery token was not found in the response.");
    }

    private static bool HasBusinessCookie(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("Set-Cookie", out var values) &&
            values.Any(value => value.Contains(".DigitalCards.Business=", StringComparison.Ordinal));
    }

    private static bool HasExpiredBusinessCookie(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("Set-Cookie", out var values) &&
            values.Any(value =>
                value.Contains(".DigitalCards.Business=", StringComparison.Ordinal) &&
                value.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }

    private static string NewLegacySafeUserName(string prefix)
    {
        return $"{prefix}{Guid.NewGuid():N}"[..12];
    }
}
