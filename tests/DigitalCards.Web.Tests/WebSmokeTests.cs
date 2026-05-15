using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using DigitalCards.Application;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Domain;
using DigitalCards.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
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
    public async Task HomePage_ReturnsLoginGateway()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Contains("Puntelio", html);
        Assert.Contains("Tarjetas de lealtad digitales", html);
        Assert.Contains("home-login-gateway", html);
        Assert.DoesNotContain("Puntelio DigitalCards", html);
        Assert.DoesNotContain("Registro cliente", html);
        Assert.DoesNotContain("home-admin-login-link", html);
        Assert.DoesNotContain("/Admin/Login", html);
        Assert.Contains("navbar-nav ms-auto", html);
        Assert.DoesNotContain("home-outbox-link", html);
        Assert.DoesNotContain("Outbox fake", html);
        Assert.DoesNotContain("Apple Wallet", html);
        Assert.DoesNotContain("Google Wallet", html);
        Assert.DoesNotContain("data-testid=\"legacy-shell\"", html);
    }

    [Fact]
    public async Task BrandConsistency_RendersSharedBrandTokensAndFooters()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient();

        var homeHtml = await client.GetStringAsync("/");
        var css = await client.GetStringAsync("/css/site.css");

        await LoginAdminAsync(client);
        var adminHtml = await client.GetStringAsync("/Admin/Dashboard");

        Assert.Contains("public-brand-lockup", homeHtml);
        Assert.Contains("Propiedad de IngeLabs", homeHtml);
        Assert.Contains("--dc-primary", css);
        Assert.Contains("--dc-radius", css);
        Assert.Contains("legacy-brand-mark brand-mark", adminHtml);
        Assert.Contains("Propiedad de IngeLabs", adminHtml);
    }

    [Fact]
    public async Task FinalVisualPolish_RendersSharedDepthAndDangerZones()
    {
        using var fake = WithFakeIntegrations();
        var adminClient = fake.Factory.CreateClient();
        var clientUserName = NewLegacySafeUserName("ui");
        const string clientPassword = "ClientPass123!";

        var css = await adminClient.GetStringAsync("/css/site.css");
        var homeHtml = await adminClient.GetStringAsync("/");

        await LoginAdminAsync(adminClient);
        var businessProfileHtml = await adminClient.GetStringAsync("/Admin/BusinessProfile/11111111-1111-1111-1111-111111111111");

        await RegisterClientAsync(fake.Factory, clientUserName, password: clientPassword);
        var client = fake.Factory.CreateClient();
        await LoginClientAsync(client, clientUserName, clientPassword);
        var clientDashboardHtml = await client.GetStringAsync("/Client/Dashboard");

        Assert.Contains("--dc-shadow-card", css);
        Assert.Contains(".danger-zone-card", css);
        Assert.Contains("home-wallet-preview", homeHtml);
        Assert.Contains("danger-zone-card", businessProfileHtml);
        Assert.Contains("client-qr-card", clientDashboardHtml);
        Assert.DoesNotContain("PasswordHash", businessProfileHtml);
        Assert.DoesNotContain("WalletSelectToken", clientDashboardHtml);
    }

    [Fact]
    public async Task LoginPages_ReturnSharedVisualAuthShell()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient();

        var adminHtml = await client.GetStringAsync("/Admin/Login");
        var businessHtml = await client.GetStringAsync("/Business/Login");
        var clientHtml = await client.GetStringAsync("/Client/Login");

        Assert.Contains("auth-gateway", adminHtml);
        Assert.Contains("auth-gateway", businessHtml);
        Assert.Contains("auth-gateway", clientHtml);
        Assert.Contains("admin-login-form", adminHtml);
        Assert.Contains("business-login-form", businessHtml);
        Assert.Contains("client-login-form", clientHtml);
        Assert.DoesNotContain("demo@digitalcards.test", businessHtml);
        Assert.DoesNotContain("auth-role-mark", adminHtml);
        Assert.DoesNotContain("auth-role-mark", businessHtml);
        Assert.DoesNotContain("auth-role-mark", clientHtml);
        Assert.DoesNotContain("Entrar como negocio", businessHtml);
        Assert.DoesNotContain("Entrar como cliente", clientHtml);
        Assert.Contains("Volver al inicio", adminHtml);
        Assert.Contains("Volver al inicio", businessHtml);
        Assert.Contains("Volver al inicio", clientHtml);
    }

    [Fact]
    public async Task Dashboards_RenderActionableEmptyStateGuidance()
    {
        using var fake = WithFakeIntegrations();

        var adminClient = fake.Factory.CreateClient();
        await LoginAdminAsync(adminClient);
        var adminHtml = await adminClient.GetStringAsync("/Admin/Dashboard");

        var businessClient = fake.Factory.CreateClient();
        await LoginBusinessAsync(businessClient);
        var businessHtml = await businessClient.GetStringAsync("/Business/Dashboard");

        var userName = NewLegacySafeUserName("empty");
        var password = "ClientPass123!";
        await RegisterClientAsync(fake.Factory, userName, $"{userName}@example.test", password);
        var client = fake.Factory.CreateClient();
        await LoginClientAsync(client, userName, password);
        var clientHtml = await client.GetStringAsync("/Client/Dashboard");

        Assert.Contains("admin-dashboard-next-steps", adminHtml);
        Assert.Contains("business-dashboard-no-cards", businessHtml);
        Assert.Contains("client-dashboard-empty-cards", clientHtml);
    }

    [Fact]
    public async Task AdminPages_RenderFinalParityVisualShell()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient();
        await LoginAdminAsync(client);

        var dashboardHtml = await client.GetStringAsync("/Admin/Dashboard");
        var businessesHtml = await client.GetStringAsync("/Admin/Businesses");
        var supportHtml = await client.GetStringAsync("/Admin/Support");
        var cutoverHtml = await client.GetStringAsync("/Admin/Cutover");

        Assert.Contains("admin-dashboard-command-strip", dashboardHtml);
        Assert.Contains("admin-businesses-panel", businessesHtml);
        Assert.Contains("admin-filter-card", businessesHtml);
        Assert.Contains("support-filter-panel", supportHtml);
        Assert.Contains("admin-businesses-panel", cutoverHtml);
        Assert.Contains("admin-support-audit-events", supportHtml);
        Assert.DoesNotContain("LegacyWalletSync", cutoverHtml);
        Assert.DoesNotContain("PasswordHash", supportHtml);
        Assert.DoesNotContain("connection string", supportHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthenticatedOperations_DoNotRenderLegacyOperationalLanguage()
    {
        using var fake = WithFakeIntegrations();
        var adminClient = fake.Factory.CreateClient();
        var businessClient = fake.Factory.CreateClient();
        var clientClient = fake.Factory.CreateClient();
        var userName = NewLegacySafeUserName("ol");
        const string clientPassword = "ClientPass123!";

        await LoginAdminAsync(adminClient);
        await LoginBusinessAsync(businessClient);
        await RegisterClientAsync(fake.Factory, userName, password: clientPassword);
        await LoginClientAsync(clientClient, userName, clientPassword);

        var pages = new[]
        {
            await adminClient.GetStringAsync("/Admin/Dashboard"),
            await adminClient.GetStringAsync("/Admin/Support"),
            await adminClient.GetStringAsync("/Admin/Businesses"),
            await businessClient.GetStringAsync("/Business/Dashboard"),
            await businessClient.GetStringAsync("/Business/Reports"),
            await clientClient.GetStringAsync("/Client/Dashboard")
        };

        foreach (var html in pages)
        {
            Assert.DoesNotContain("LegacyWalletSync", html);
            Assert.DoesNotContain("LegacySync", html);
            Assert.DoesNotContain("Web Forms", html);
            Assert.DoesNotContain("fallback", html, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task BusinessPages_RenderOperationsUx()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient();
        await LoginBusinessAsync(client);

        var dashboardHtml = await client.GetStringAsync("/Business/Dashboard");
        var cardsHtml = await client.GetStringAsync("/Business/Cards");
        var checkInResponse = await client.GetAsync("/Business/CheckIn");

        Assert.DoesNotContain("business-operation-strip", dashboardHtml);
        Assert.DoesNotContain("Modo mostrador", dashboardHtml);
        Assert.Contains("business-primary-workflow", cardsHtml);
        Assert.Contains("business-card-detail-empty", cardsHtml);
        Assert.Contains("business-card-detail-empty-panel", cardsHtml);
        Assert.Contains("<h2>Detalle</h2>", cardsHtml);
        Assert.Equal("/Business/Cards", checkInResponse.RequestMessage?.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task ClientPages_RenderFinalExperienceUx()
    {
        using var fake = WithFakeIntegrations();
        var userName = NewLegacySafeUserName("cux");
        const string password = "ClientPass123!";
        await RegisterClientAsync(fake.Factory, userName, $"{userName}@example.test", password);

        var client = fake.Factory.CreateClient();
        await LoginClientAsync(client, userName, password);

        var dashboardHtml = await client.GetStringAsync("/Client/Dashboard");
        var cardsHtml = await client.GetStringAsync("/Client/Cards");
        var profileHtml = await client.GetStringAsync("/Client/Profile");

        Assert.Contains("client-wallet-guide", dashboardHtml);
        Assert.DoesNotContain("client-action-grid", dashboardHtml);
        Assert.DoesNotContain("client-dashboard-cards-link", dashboardHtml);
        Assert.DoesNotContain("client-dashboard-profile-link", dashboardHtml);
        Assert.DoesNotContain("client-logout-link", dashboardHtml);
        Assert.Contains("client-sidebar-dashboard-link", dashboardHtml);
        Assert.Contains("client-sidebar-cards-link", dashboardHtml);
        Assert.Contains("client-sidebar-profile-link", dashboardHtml);
        Assert.Contains("client-layout-logout-link", dashboardHtml);
        Assert.Contains("client-cards-empty-state", cardsHtml);
        Assert.Contains("client-profile-helper", profileHtml);
        Assert.DoesNotContain("Sellos historicos", dashboardHtml);
        Assert.DoesNotContain("Sellos historicos", cardsHtml);
        Assert.DoesNotContain("PasswordHash", dashboardHtml);
        Assert.DoesNotContain("WalletSelectToken", cardsHtml);
    }

    [Fact]
    public async Task DevOutboxRoute_IsRemovedAndLinksAreHidden()
    {
        using var fake = WithFakeIntegrations(environmentName: "Development");
        var client = fake.Factory.CreateClient();

        var response = await client.GetAsync("/Dev/Outbox");
        var homeHtml = await client.GetStringAsync("/");

        await LoginBusinessAsync(client);
        var dashboardHtml = await client.GetStringAsync("/Business/Dashboard");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("home-outbox-link", homeHtml);
        Assert.DoesNotContain("dashboard-outbox-link", dashboardHtml);
        Assert.DoesNotContain("Outbox fake", homeHtml);
        Assert.DoesNotContain("Ver outbox", dashboardHtml);
    }

    [Fact]
    public async Task SecurityHeaders_AreAppliedAndAuthPagesAreNoStore()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient();

        var home = await client.GetAsync("/");
        var login = await client.GetAsync("/Business/Login");

        Assert.Equal("DENY", home.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("nosniff", home.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("strict-origin-when-cross-origin", home.Headers.GetValues("Referrer-Policy").Single());
        Assert.Contains("no-store", login.Headers.CacheControl?.ToString());
        Assert.Contains(login.Headers.Pragma, value => value.Name == "no-cache");
    }

    [Fact]
    public async Task RateLimits_ProtectAuthPagesWithoutBlockingWalletPublicPolicy()
    {
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:Security:RateLimiting:AuthPermitLimit"] = "1",
            ["DigitalCards:Security:RateLimiting:AuthWindowSeconds"] = "60",
            ["DigitalCards:Security:RateLimiting:WalletPermitLimit"] = "5",
            ["DigitalCards:Security:RateLimiting:WalletWindowSeconds"] = "60"
        });
        var client = fake.Factory.CreateClient();
        var token = await CreateEnrollmentTokenAsync(fake.Factory, NewLegacySafeUserName("rl"));

        var firstLogin = await client.GetAsync("/Business/Login");
        var secondLogin = await client.GetAsync("/Business/Login");
        var firstWallet = await client.GetAsync($"/Wallet/Select/{token}");
        var secondWallet = await client.GetAsync($"/Wallet/Select/{token}");

        Assert.Equal(HttpStatusCode.OK, firstLogin.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondLogin.StatusCode);
        Assert.Equal(HttpStatusCode.OK, firstWallet.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondWallet.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedRolePages_UseLegacyWebFormsShell()
    {
        using var fake = WithFakeIntegrations();
        var adminClient = fake.Factory.CreateClient();
        var businessClient = fake.Factory.CreateClient();
        var clientClient = fake.Factory.CreateClient();
        var userName = NewLegacySafeUserName("ls");
        const string clientPassword = "ClientPass123!";

        await LoginAdminAsync(adminClient);
        var adminHtml = await adminClient.GetStringAsync("/Admin/Dashboard");

        Assert.Contains("data-testid=\"legacy-shell\"", adminHtml);
        Assert.Contains("Administradores", adminHtml);
        Assert.Contains("/Admin/Clients", adminHtml);
        Assert.Contains("Propiedad de IngeLabs", adminHtml);
        Assert.Contains("data-legacy-menu-button", adminHtml);
        Assert.Contains("data-legacy-sidebar-backdrop", adminHtml);
        Assert.Contains("legacy-sidebar-icon", adminHtml);
        Assert.DoesNotContain("data-legacy-menu-button disabled", adminHtml, StringComparison.OrdinalIgnoreCase);

        await LoginBusinessAsync(businessClient);
        var businessHtml = await businessClient.GetStringAsync("/Business/Dashboard");

        Assert.Contains("data-testid=\"legacy-shell\"", businessHtml);
        Assert.Contains("Duenos de negocios", businessHtml);
        Assert.Contains("Tarjetas", businessHtml);
        Assert.Contains("Checadas", businessHtml);
        Assert.DoesNotContain("Mostrador", businessHtml);

        await RegisterClientAsync(fake.Factory, userName, password: clientPassword);
        await LoginClientAsync(clientClient, userName, clientPassword);
        var clientHtml = await clientClient.GetStringAsync("/Client/Dashboard");

        Assert.Contains("data-testid=\"legacy-shell\"", clientHtml);
        Assert.Contains("Cliente", clientHtml);
        Assert.Contains("Mis tarjetas", clientHtml);
    }

    [Fact]
    public async Task AdminPages_UseLegacyAdminListPresentation()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient();

        await LoginAdminAsync(client);
        var dashboardHtml = await client.GetStringAsync("/Admin/Dashboard");
        var businessesHtml = await client.GetStringAsync("/Admin/Businesses?Query=demo");
        var adminsHtml = await client.GetStringAsync("/Admin/AdminUsers");

        Assert.Contains("admin-action-grid", dashboardHtml);
        Assert.Contains("admin-clients-link", dashboardHtml);
        Assert.Contains("limpia cuentas de prueba", dashboardHtml);
        Assert.Contains("legacy-admin-list", businessesHtml);
        Assert.Contains("legacy-admin-row", businessesHtml);
        Assert.Contains("legacy-admin-list", adminsHtml);
        Assert.Contains("legacy-admin-row", adminsHtml);
    }

    [Fact]
    public async Task AdminCutover_RequiresAdminCookie()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Admin/Cutover");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Admin/Login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task AdminCutover_ShowsReadinessAndCanChangeActivationStatus()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient();

        await LoginAdminAsync(client);
        using var scope = fake.Factory.Services.CreateScope();
        var businesses = scope.ServiceProvider.GetRequiredService<IBusinessRepository>();
        var business = await businesses.FindByEmailAsync("demo@digitalcards.test");
        Assert.NotNull(business);

        var profilePath = $"/Admin/BusinessProfile/{business!.Id}";
        var html = await client.GetStringAsync(profilePath);
        var token = ExtractAntiforgeryToken(html);

        Assert.Contains("admin-business-operational-panel", html);
        Assert.Contains("Demo Coffee", html);
        Assert.Contains("admin-business-profile-readiness", html);
        Assert.DoesNotContain("LegacyWalletSync", html);

        var response = await client.PostAsync(
            $"{profilePath}?handler=Save",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.BusinessName"] = "Demo Coffee",
                ["Input.BusinessEmail"] = "demo@digitalcards.test",
                ["Input.BusinessLogo"] = "/img/demo-coffee.svg",
                ["Input.ActivationStatus"] = "ModernPrimary",
                ["__RequestVerificationToken"] = token
            }));
        var updatedHtml = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("admin-business-profile-status", updatedHtml);
        Assert.Contains("Activo", updatedHtml);
        Assert.DoesNotContain("business123", updatedHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionString", updatedHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdminCutover_CanRecordSmokeEvidenceWithoutSecrets()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient();

        await LoginAdminAsync(client);
        using var scope = fake.Factory.Services.CreateScope();
        var businesses = scope.ServiceProvider.GetRequiredService<IBusinessRepository>();
        var business = await businesses.FindByEmailAsync("demo@digitalcards.test");
        Assert.NotNull(business);

        var profilePath = $"/Admin/BusinessProfile/{business!.Id}";
        var html = await client.GetStringAsync(profilePath);
        var token = ExtractAntiforgeryToken(html);

        var response = await client.PostAsync(
            $"{profilePath}?handler=Smoke",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["SmokeInput.HealthOk"] = "true",
                ["SmokeInput.ReadyOk"] = "true",
                ["SmokeInput.EmailOk"] = "true",
                ["SmokeInput.WalletMobileOk"] = "true",
                ["SmokeInput.WalletSavedOk"] = "true",
                ["SmokeInput.ModernStampOk"] = "true",
                ["SmokeInput.SupportReviewed"] = "true",
                ["SmokeInput.Notes"] = "validado con iPhone controlado",
                ["__RequestVerificationToken"] = token
            }));
        var updatedHtml = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("admin-business-profile-smoke-evidence", updatedHtml);
        Assert.Contains("Smoke de activacion registrado como completo", updatedHtml);
        Assert.Contains("validado con iPhone controlado", updatedHtml);
        Assert.DoesNotContain("business123", updatedHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionString", updatedHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("push token", updatedHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PublicBusinessEnrollment_UsesOpaqueBusinessTokenAndSendsWalletEmail()
    {
        using var fake = WithFakeIntegrations();
        var admin = fake.Factory.CreateClient();
        var businessId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userName = NewLegacySafeUserName("pbe");

        await LoginAdminAsync(admin);
        var profileHtml = await admin.GetStringAsync($"/Admin/BusinessProfile/{businessId}");
        var profileToken = ExtractAntiforgeryToken(profileHtml);
        var linkResponse = await admin.PostAsync(
            $"/Admin/BusinessProfile/{businessId}?handler=GenerateEnrollmentLink",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = profileToken
            }));
        var linkHtml = await linkResponse.Content.ReadAsStringAsync();
        var businessToken = ExtractBusinessEnrollmentToken(linkHtml);

        using (var scope = fake.Factory.Services.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<InMemoryDigitalCardsStore>();
            var storedToken = Assert.Single(store.BusinessEnrollmentLinks);
            Assert.NotEqual(businessToken, storedToken.TokenHash);
            Assert.Equal(businessToken[^8..], storedToken.TokenSuffix);
        }

        var publicClient = fake.Factory.CreateClient();
        var publicHtml = await publicClient.GetStringAsync($"/Enroll/{businessToken}");
        var publicCsrf = ExtractAntiforgeryToken(publicHtml);

        Assert.Contains("public-business-enrollment-page", publicHtml);
        Assert.Contains("Demo Coffee", publicHtml);

        var registerResponse = await publicClient.PostAsync(
            $"/Enroll/{businessToken}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.UserName"] = userName,
                ["Input.FirstName"] = "Public",
                ["Input.LastName"] = "Enroll",
                ["Input.Email"] = $"{userName}@example.test",
                ["Input.Password"] = "ClientPass123!",
                ["Input.AcceptTerms"] = "true",
                ["__RequestVerificationToken"] = publicCsrf
            }));
        var registerHtml = await registerResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        Assert.Contains("public-business-enrollment-success", registerHtml);
        Assert.Contains("public-business-enrollment-wallet-link", registerHtml);
        Assert.DoesNotContain("ClientPass123!", registerHtml);

        using var outboxScope = fake.Factory.Services.CreateScope();
        var outbox = outboxScope.ServiceProvider.GetRequiredService<IWalletEmailOutbox>();
        var messages = await outbox.ListAsync();
        Assert.Contains(messages, message => string.Equals(message.To, $"{userName}@example.test", StringComparison.Ordinal));

        var consentStore = outboxScope.ServiceProvider.GetRequiredService<InMemoryDigitalCardsStore>();
        var client = consentStore.Clients.Single(item => item.UserName == userName);
        var consent = Assert.Single(consentStore.ClientConsents, item => item.ClientId == client.Id);
        Assert.Equal("privacy-2026-05", consent.PolicyVersion);
        Assert.Equal("PublicBusinessEnrollment", consent.Source);
    }

    [Fact]
    public async Task BusinessDashboard_GeneratesPublicEnrollmentQr()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient();

        await LoginBusinessAsync(client);
        var dashboardHtml = await client.GetStringAsync("/Business/Dashboard");
        var csrf = ExtractAntiforgeryToken(dashboardHtml);

        var response = await client.PostAsync(
            "/Business/Dashboard?handler=GenerateEnrollmentLink",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = csrf
            }));
        var html = await response.Content.ReadAsStringAsync();
        var businessToken = ExtractBusinessEnrollmentToken(html);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("business-enrollment-qr-result", html);
        Assert.Contains("business-enrollment-qr", html);
        Assert.Contains("<svg", html, StringComparison.OrdinalIgnoreCase);

        using var scope = fake.Factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<InMemoryDigitalCardsStore>();
        var storedToken = Assert.Single(store.BusinessEnrollmentLinks);
        Assert.NotEqual(businessToken, storedToken.TokenHash);
        Assert.Equal(businessToken[^8..], storedToken.TokenSuffix);
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
    public async Task HealthEndpoint_DoesNotRequireReadinessDependencies()
    {
        using var fake = WithFakeIntegrations(MySqlUnavailableOverrides());
        var client = fake.Factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ReadinessEndpoint_WithFakeIntegrations_ReturnsHealthyJson()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient();

        var response = await client.GetAsync("/health/ready");
        var json = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("\"status\":\"Healthy\"", json);
        Assert.Contains("\"name\":\"configuration\"", json);
        Assert.Contains("\"name\":\"mysql\"", json);
    }

    [Fact]
    public async Task ReadinessEndpoint_WhenMySqlUnavailable_DoesNotExposeConnectionString()
    {
        using var fake = WithFakeIntegrations(MySqlUnavailableOverrides());
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/health/ready");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("\"status\":\"Unhealthy\"", json);
        Assert.Contains("MySQL readiness query failed.", json);
        Assert.DoesNotContain("SUPER_SECRET", json);
        Assert.DoesNotContain("ConnectionStrings", json);
        Assert.DoesNotContain("Server=127.0.0.1", json);
    }

    [Fact]
    public async Task ForwardedHeaders_MarkAuthCookieSecure_WhenOriginalSchemeIsHttps()
    {
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:Operations:TrustAllForwardedHeaders"] = "true"
        });
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Forwarded-Proto", "https");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Forwarded-Host", "app.puntelio.com");

        var response = await LoginBusinessAsync(client);

        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var values));
        Assert.Contains(values, value =>
            value.Contains(".DigitalCards.Business=", StringComparison.Ordinal) &&
            value.Contains("secure", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DataProtectionKeys_WhenPersisted_KeepBusinessCookieValidAfterRestart()
    {
        var keysPath = Path.Combine(Path.GetTempPath(), $"digitalcards-dp-{Guid.NewGuid():N}");
        var overrides = new Dictionary<string, string?>
        {
            ["DigitalCards:Operations:DataProtectionKeysPath"] = keysPath,
            ["DigitalCards:Operations:RequireDataProtectionKeysForReadiness"] = "true"
        };

        try
        {
            string businessCookie;
            using (var first = WithFakeIntegrations(overrides))
            {
                var client = first.Factory.CreateClient(new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });
                var response = await LoginBusinessAsync(client);
                businessCookie = response.Headers
                    .GetValues("Set-Cookie")
                    .Single(value => value.Contains(".DigitalCards.Business=", StringComparison.Ordinal))
                    .Split(';')[0];
            }

            using var second = WithFakeIntegrations(overrides);
            var restartedClient = second.Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            restartedClient.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", businessCookie);

            var dashboard = await restartedClient.GetAsync("/Business/Dashboard");

            Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        }
        finally
        {
            if (Directory.Exists(keysPath))
            {
                Directory.Delete(keysPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BusinessDashboard_WithoutCookie_RedirectsToLogin()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        foreach (var path in new[] { "/Business/Dashboard", "/Business/Reports" })
        {
            var response = await client.GetAsync(path);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Business/Login", response.Headers.Location?.OriginalString);
        }
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
    public async Task AdminPages_WithoutCookie_RedirectToLogin()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        foreach (var path in new[] { "/Admin/Dashboard", "/Admin/Businesses", "/Admin/CreateBusiness", "/Admin/BusinessProfile/11111111-1111-1111-1111-111111111111", "/Admin/AdminUsers", "/Admin/CreateAdmin", "/Admin/Clients", "/Admin/Reports", "/Admin/Support", "/Admin/Audit" })
        {
            var response = await client.GetAsync(path);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Admin/Login", response.Headers.Location?.OriginalString);
        }
    }

    [Fact]
    public async Task AdminLogin_WithValidLegacyAdminCredentials_EmitsAdminCookie()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await LoginAdminAsync(client);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Admin/Dashboard", response.Headers.Location?.OriginalString);
        Assert.True(HasAdminCookie(response));
        Assert.False(HasBusinessCookie(response));
    }

    [Fact]
    public async Task AdminLogin_WithBusinessCredentials_DoesNotEmitAdminCookie()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await LoginAdminAsync(
            client,
            userNameOrEmail: "demo@digitalcards.test",
            password: "business123");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Credenciales de admin invalidas", html);
        Assert.False(HasAdminCookie(response));
    }

    [Fact]
    public async Task AdminBusinesses_EnableAndDisablePilotBusinessControlsModernAccess()
    {
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:Pilot:Enabled"] = "true"
        });
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginBusinessAsync(client);
        var blockedHtml = await client.GetStringAsync("/Business/Dashboard");
        Assert.Contains("pilot-business-blocked", blockedHtml);

        await LoginAdminAsync(client);
        var businessesBefore = await client.GetStringAsync("/Admin/Businesses");
        Assert.Contains("admin-business-row-actions", businessesBefore);
        Assert.Contains("admin-manage-business", businessesBefore);
        Assert.Contains("admin-enable-pilot", businessesBefore);
        Assert.DoesNotContain("admin-disable-pilot", businessesBefore);
        var enableToken = await GetAntiforgeryTokenAsync(client, "/Admin/Businesses");
        var enableResponse = await client.PostAsync(
            "/Admin/Businesses?handler=Enable",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["businessId"] = "11111111-1111-1111-1111-111111111111",
                ["notes"] = "piloto web test",
                ["__RequestVerificationToken"] = enableToken
            }));
        var enableHtml = await enableResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
        Assert.Contains("Negocio activado", enableHtml);
        Assert.Contains("admin-business-row-actions", enableHtml);
        Assert.Contains("admin-manage-business", enableHtml);
        Assert.Contains("admin-disable-pilot", enableHtml);
        Assert.DoesNotContain("admin-enable-pilot", enableHtml);

        var allowedHtml = await client.GetStringAsync("/Business/Dashboard");
        Assert.DoesNotContain("pilot-business-blocked", allowedHtml);
        Assert.Contains("cards-link", allowedHtml);

        var disableToken = ExtractAntiforgeryToken(enableHtml);
        var disableResponse = await client.PostAsync(
            "/Admin/Businesses?handler=Disable",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["businessId"] = "11111111-1111-1111-1111-111111111111",
                ["__RequestVerificationToken"] = disableToken
            }));
        var disableHtml = await disableResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, disableResponse.StatusCode);
        Assert.Contains("Negocio desactivado", disableHtml);

        var blockedAgainHtml = await client.GetStringAsync("/Business/Dashboard");
        Assert.Contains("pilot-business-blocked", blockedAgainHtml);
    }

    [Fact]
    public async Task AdminClients_ShowsClientConsoleAndCleanup()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var userName = NewLegacySafeUserName("pc");
        var email = $"{userName}@blocked.test";
        await CreateEnrollmentAsync(fake.Factory, userName, email);

        await LoginAdminAsync(client);
        var searchHtml = await client.GetStringAsync($"/Admin/Clients?Query={userName}");
        Assert.Contains("admin-client-row", searchHtml);
        Assert.Contains("admin-client-card-list", searchHtml);
        Assert.Contains("admin-client-card-row", searchHtml);
        Assert.Contains("Demo Coffee", searchHtml);
        Assert.Contains("Pendiente", searchHtml);
        Assert.Contains(userName, searchHtml);
        Assert.Contains(email, searchHtml);
        Assert.DoesNotContain("password", searchHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash", searchHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("admin-enable-client-pilot", searchHtml);
        Assert.DoesNotContain("admin-disable-client-pilot", searchHtml);
        Assert.Contains("admin-client-delete-form", searchHtml);
        Assert.Contains("admin-client-cleanup-note", searchHtml);
        Assert.Contains("Clientes y limpieza de pruebas", searchHtml);
        Assert.Contains("no borra negocios", searchHtml);
        Assert.Contains("Eliminar cliente borra cuenta global, tarjetas, links Wallet y datos relacionados; no borra negocios.", searchHtml);
    }

    [Fact]
    public async Task AdminBusinessPages_HideNotesAndBrandingUsesRewardLabel()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginAdminAsync(client);
        var businessesHtml = await client.GetStringAsync("/Admin/Businesses?Query=demo");
        var createHtml = await client.GetStringAsync("/Admin/CreateBusiness");
        var profileHtml = await client.GetStringAsync("/Admin/BusinessProfile/11111111-1111-1111-1111-111111111111");

        Assert.DoesNotContain("admin-business-notes", businessesHtml);
        Assert.DoesNotContain("admin-create-business-notes", createHtml);
        Assert.DoesNotContain("admin-business-profile-notes", profileHtml);
        Assert.Contains("Recompensa", profileHtml);
        Assert.DoesNotContain(">Descripcion<", profileHtml);
        Assert.Contains(">Guardar<", profileHtml);
        Assert.Contains("Guardar y actualizar", profileHtml);
        Assert.Contains("Actualizar Tarjetas", profileHtml);
        Assert.Contains(">Actualizar<", profileHtml);
        Assert.DoesNotContain("Refrescar Wallets recientes", profileHtml);
        Assert.DoesNotContain("admin-business-wallet-branding-refresh-limit", profileHtml);
    }

    [Fact]
    public async Task AdminClients_CanPermanentlyDeleteClientCardsAndWalletData()
    {
        using var fake = WithFakeIntegrations();
        var http = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var userName = NewLegacySafeUserName("dc");
        var enrollment = await CreateEnrollmentAsync(fake.Factory, userName);
        var walletToken = ExtractWalletToken(enrollment.EnrollmentUrl);
        Guid clientId;

        using (var scope = fake.Factory.Services.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<InMemoryDigitalCardsStore>();
            lock (store.Sync)
            {
                clientId = store.Clients.Single(client => client.UserName == userName).Id;
                var businessId = store.Businesses.Single(business => business.Name == "Demo Coffee").Id;
                store.ClientCredentials.Add(new ClientCredential(clientId, "modern-client-hash", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
                store.ClientConsents.Add(new ClientConsent(1, clientId, businessId, "privacy-test", "Web", DateTimeOffset.UtcNow));
                store.PasswordResetTokens.Add(new PasswordResetTokenRecord(
                    1,
                    PasswordResetAccountType.Client,
                    clientId,
                    "modern-reset-token-hash",
                    "suffix",
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow.AddHours(1),
                    UsedAt: null,
                    RevokedAt: null));
            }
        }

        await LoginAdminAsync(http);
        var searchHtml = await http.GetStringAsync($"/Admin/Clients?Query={userName}");
        var token = ExtractAntiforgeryToken(searchHtml);

        var response = await http.PostAsync(
            $"/Admin/Clients?handler=Delete&clientId={clientId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["confirmation"] = userName,
                ["__RequestVerificationToken"] = token
            }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Cliente eliminado permanentemente", html);
        Assert.DoesNotContain(userName, html);

        using (var verifyScope = fake.Factory.Services.CreateScope())
        {
            var store = verifyScope.ServiceProvider.GetRequiredService<InMemoryDigitalCardsStore>();
            lock (store.Sync)
            {
                Assert.DoesNotContain(store.Clients, client => client.Id == clientId);
                Assert.DoesNotContain(store.LoyaltyCards, card => card.ClientId == clientId);
                Assert.DoesNotContain(store.WalletLinkTokens, tokenRecord => tokenRecord.CardId == enrollment.Card.Id);
                Assert.DoesNotContain(store.ClientCredentials, credential => credential.ClientId == clientId);
                Assert.DoesNotContain(store.ClientConsents, consent => consent.ClientId == clientId);
                Assert.DoesNotContain(store.PasswordResetTokens, resetToken => resetToken.AccountId == clientId);
                Assert.Contains(store.Businesses, business => business.Name == "Demo Coffee");
                Assert.Contains(store.AuditEvents, audit =>
                    audit.EventType == OperationalAuditEventType.ClientDeleted &&
                    audit.ClientId == clientId);
            }
        }

        var walletHtml = await http.GetStringAsync($"/Wallet/Select/{walletToken}");
        Assert.Contains("wallet-not-found", walletHtml);
    }

    [Fact]
    public async Task AdminSupport_SearchesCardsWalletStateAndLedgerWithoutSecrets()
    {
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:LegacyWalletSync:Enabled"] = "true"
        });
        var http = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var userName = NewLegacySafeUserName("su");
        var enrollment = await CreateEnrollmentAsync(fake.Factory, userName);
        using (var scope = fake.Factory.Services.CreateScope())
        {
            var app = scope.ServiceProvider.GetRequiredService<DigitalCardsAppService>();
            var store = scope.ServiceProvider.GetRequiredService<InMemoryDigitalCardsStore>();
            var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
                "demo@digitalcards.test",
                "business123"));
            await app.SelectGoogleWalletAsync(ExtractWalletToken(enrollment.EnrollmentUrl));
            await app.AddStampToCardAsync(business!.Id, enrollment.Card.Id);
            var card = store.LoyaltyCards.Single(item => item.Id == enrollment.Card.Id);
            store.StampLedger.Add(new StampLedgerRecord(
                9901,
                card.Id,
                card.BusinessId,
                card.ClientId,
                StampLedgerSource.LegacySync,
                null,
                card.CurrentStamps - 1,
                card.CurrentStamps,
                card.LifetimeStamps - 1,
                card.LifetimeStamps,
                card.LastStampedAt,
                GoogleWalletAttempted: true,
                GoogleWalletSucceeded: false,
                AppleWalletAttempted: false,
                AppleWalletSucceeded: false,
                "Tarjeta digital fallo seguro",
                DateTimeOffset.UtcNow));
        }

        await LoginAdminAsync(http);
        var html = await http.GetStringAsync($"/Admin/Support?Query={userName}");

        Assert.Contains("admin-support-results", html);
        Assert.Contains("admin-support-filters", html);
        Assert.DoesNotContain("LegacyWalletSync", html);
        Assert.Contains(userName, html);
        Assert.Contains("Demo Coffee", html);
        Assert.Contains("Emitida", html);
        Assert.Contains("admin-support-operational-state", html);
        Assert.Contains("Errores recientes seguros", html);
        Assert.Contains("Tarjeta digital fallo seguro", html);
        Assert.DoesNotContain("LegacySync", html);
        Assert.Contains("ModernBusiness", html);
        Assert.Contains("Sellos:", html);
        Assert.DoesNotContain(enrollment.Card.EnrollmentToken, html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("business123", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("push-token", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("auth-token", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdminSupport_RetryWalletUpdate_RecordsAdminRetryWithoutSecrets()
    {
        using var fake = WithFakeIntegrations();
        var http = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var userName = NewLegacySafeUserName("sr");
        var enrollment = await CreateEnrollmentAsync(fake.Factory, userName);
        using (var scope = fake.Factory.Services.CreateScope())
        {
            var app = scope.ServiceProvider.GetRequiredService<DigitalCardsAppService>();
            await app.SelectGoogleWalletAsync(ExtractWalletToken(enrollment.EnrollmentUrl));
        }

        await LoginAdminAsync(http);
        var supportHtml = await http.GetStringAsync($"/Admin/Support?Query={userName}");
        var csrf = ExtractAntiforgeryToken(supportHtml);

        var response = await http.PostAsync(
            "/Admin/Support?handler=RetryWalletUpdate",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["RetryCardId"] = enrollment.Card.Id.ToString("D"),
                ["Query"] = userName,
                ["__RequestVerificationToken"] = csrf
            }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("admin-support-status-message", html);
        Assert.Contains("AdminRetry", html);
        Assert.Contains("Tarjeta: Actualizada", html);
        Assert.DoesNotContain(enrollment.Card.EnrollmentToken, html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("auth-token", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("push-token", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdminSupport_ExportsSafeDiagnosticsJsonWithoutSecrets()
    {
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:LegacyWalletSync:Enabled"] = "true"
        });
        var http = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var userName = NewLegacySafeUserName("sx");
        var enrollment = await CreateEnrollmentAsync(fake.Factory, userName);
        using (var scope = fake.Factory.Services.CreateScope())
        {
            var app = scope.ServiceProvider.GetRequiredService<DigitalCardsAppService>();
            var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
                "demo@digitalcards.test",
                "business123"));
            await app.SelectGoogleWalletAsync(ExtractWalletToken(enrollment.EnrollmentUrl));
            await app.AddStampToCardAsync(business!.Id, enrollment.Card.Id);
        }

        await LoginAdminAsync(http);
        var response = await http.GetAsync($"/Admin/Support?handler=Export&Query={userName}");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Contains("support-diagnostic-", response.Content.Headers.ContentDisposition?.FileName);
        Assert.DoesNotContain("legacyWalletSync", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(userName, json);
        Assert.Contains("Demo Coffee", json);
        Assert.Contains("ModernBusiness", json);
        Assert.DoesNotContain(enrollment.Card.EnrollmentToken, json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("business123", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("push-token", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("auth-token", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionStrings", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdminSupport_ExportsSafeDiagnosticsCsvWithFiltersWithoutSecrets()
    {
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:LegacyWalletSync:Enabled"] = "true"
        });
        var http = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var userName = NewLegacySafeUserName("sc");
        var enrollment = await CreateEnrollmentAsync(fake.Factory, userName);
        using (var scope = fake.Factory.Services.CreateScope())
        {
            var app = scope.ServiceProvider.GetRequiredService<DigitalCardsAppService>();
            var store = scope.ServiceProvider.GetRequiredService<InMemoryDigitalCardsStore>();
            var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
                "demo@digitalcards.test",
                "business123"));
            await app.SelectGoogleWalletAsync(ExtractWalletToken(enrollment.EnrollmentUrl));
            await app.AddStampToCardAsync(business!.Id, enrollment.Card.Id);
            var card = store.LoyaltyCards.Single(item => item.Id == enrollment.Card.Id);
            store.StampLedger.Add(new StampLedgerRecord(
                9902,
                card.Id,
                card.BusinessId,
                card.ClientId,
                StampLedgerSource.LegacySync,
                null,
                card.CurrentStamps - 1,
                card.CurrentStamps,
                card.LifetimeStamps - 1,
                card.LifetimeStamps,
                card.LastStampedAt,
                GoogleWalletAttempted: true,
                GoogleWalletSucceeded: false,
                AppleWalletAttempted: false,
                AppleWalletSucceeded: false,
                "Tarjeta digital fallo seguro",
                DateTimeOffset.UtcNow));
        }

        await LoginAdminAsync(http);
        var response = await http.GetAsync($"/Admin/Support?handler=ExportCsv&ClientFilter={userName}&BusinessFilter=Demo&WalletIssuesOnly=true");
        var csv = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Contains("support-diagnostic-", response.Content.Headers.ContentDisposition?.FileName);
        Assert.Contains("CardSuffix,ClientUserName,BusinessName", csv);
        Assert.Contains(userName, csv);
        Assert.Contains("Demo Coffee", csv);
        Assert.Contains(",True,", csv);
        Assert.DoesNotContain(enrollment.Card.EnrollmentToken, csv, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("business123", csv, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("push-token", csv, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("auth-token", csv, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", csv, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionStrings", csv, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdminAudit_RequiresAdminAndShowsSupportExportEventWithoutSecrets()
    {
        using var fake = WithFakeIntegrations();
        var anonymous = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var http = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var userName = NewLegacySafeUserName("au");
        var enrollment = await CreateEnrollmentAsync(fake.Factory, userName);

        var anonymousResponse = await anonymous.GetAsync("/Admin/Audit");

        await LoginAdminAsync(http);
        var exportResponse = await http.GetAsync($"/Admin/Support?handler=Export&Query={userName}");
        var auditHtml = await http.GetStringAsync($"/Admin/Support?AuditEventType={OperationalAuditEventType.SupportExported}");

        Assert.Equal(HttpStatusCode.Redirect, anonymousResponse.StatusCode);
        Assert.Contains("/Admin/Login", anonymousResponse.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        Assert.Contains("admin-support-audit-table", auditHtml);
        Assert.Contains("SupportExported", auditHtml);
        Assert.DoesNotContain(enrollment.Card.EnrollmentToken, auditHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("business123", auditHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", auditHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionStrings", auditHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdminReports_ShowsOperationalSummaryWithoutSecrets()
    {
        using var fake = WithFakeIntegrations();
        var http = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var userName = NewLegacySafeUserName("rp");
        var enrollment = await CreateEnrollmentAsync(fake.Factory, userName);
        using (var scope = fake.Factory.Services.CreateScope())
        {
            var app = scope.ServiceProvider.GetRequiredService<DigitalCardsAppService>();
            var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
                "demo@digitalcards.test",
                "business123"));
            await app.SelectGoogleWalletAsync(ExtractWalletToken(enrollment.EnrollmentUrl));
            await app.AddStampToCardAsync(business!.Id, enrollment.Card.Id);
        }

        await LoginAdminAsync(http);
        var html = await http.GetStringAsync("/Admin/Reports");

        Assert.Contains("admin-reports", html);
        Assert.Contains("admin-report-business-count", html);
        Assert.Contains("admin-report-recent-card", html);
        Assert.Contains(userName, html);
        Assert.Contains("Demo Coffee", html);
        Assert.DoesNotContain(enrollment.Card.EnrollmentToken, html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("business123", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionStrings", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdminCreateBusiness_DefaultsInviteEmailChecked()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginAdminAsync(client);

        var html = await client.GetStringAsync("/Admin/CreateBusiness");

        Assert.Matches(
            new Regex("data-testid=\"admin-create-business-send-invite\"[^>]*checked", RegexOptions.IgnoreCase),
            html);
    }

    [Fact]
    public async Task AdminCreateBusiness_CreatesBusinessWithModernCredentialAndPilotAccess()
    {
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:Pilot:Enabled"] = "true"
        });
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var suffix = NewLegacySafeUserName("nb");
        var businessName = $"Biz {suffix[..8]}";
        var businessEmail = $"{suffix}@biz.test";
        const string password = "StartPass123!";

        await LoginAdminAsync(client);
        var token = await GetAntiforgeryTokenAsync(client, "/Admin/CreateBusiness");
        var response = await client.PostAsync(
            "/Admin/CreateBusiness",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.BusinessName"] = businessName,
                ["Input.BusinessEmail"] = businessEmail,
                ["Input.InitialPassword"] = password,
                ["Input.ConfirmPassword"] = password,
                ["Input.EnablePilot"] = "true",
                ["Input.Notes"] = "negocio creado en web test",
                ["__RequestVerificationToken"] = token
            }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("admin-create-business-status", html);
        Assert.Contains("Activo", html);
        Assert.Contains(businessName, html);
        Assert.DoesNotContain(password, html, StringComparison.Ordinal);

        using (var scope = fake.Factory.Services.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<InMemoryDigitalCardsStore>();
            var business = store.Businesses.Single(existing => existing.Email == businessEmail);
            Assert.Equal(25, business.PasswordHashPlaceholder.Length);
            Assert.DoesNotContain(password, business.PasswordHashPlaceholder, StringComparison.Ordinal);
            Assert.Contains(store.BusinessCredentials, credential => credential.BusinessId == business.Id);
            Assert.Contains(store.PilotBusinesses, pilot => pilot.BusinessId == business.Id && pilot.IsEnabled);
        }

        var loginResponse = await LoginBusinessAsync(client, businessEmail, password);
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        var dashboardHtml = await client.GetStringAsync("/Business/Dashboard");

        Assert.Contains(businessName, dashboardHtml);
        Assert.DoesNotContain("pilot-business-blocked", dashboardHtml);
    }

    [Fact]
    public async Task AdminCreateBusiness_WithoutPilot_CreatesBlockedBusinessWhenPilotIsEnabled()
    {
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:Pilot:Enabled"] = "true"
        });
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var suffix = NewLegacySafeUserName("db");
        var businessName = $"Biz {suffix[..8]}";
        var businessEmail = $"{suffix}@biz.test";
        const string password = "StartPass123!";

        await LoginAdminAsync(client);
        var token = await GetAntiforgeryTokenAsync(client, "/Admin/CreateBusiness");
        var response = await client.PostAsync(
            "/Admin/CreateBusiness",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.BusinessName"] = businessName,
                ["Input.BusinessEmail"] = businessEmail,
                ["Input.InitialPassword"] = password,
                ["Input.ConfirmPassword"] = password,
                ["Input.EnablePilot"] = "false",
                ["__RequestVerificationToken"] = token
            }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Negocio creado", html);
        Assert.Contains("Inactivo", html);
        Assert.DoesNotContain(password, html, StringComparison.Ordinal);

        var loginResponse = await LoginBusinessAsync(client, businessEmail, password);
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        var dashboardHtml = await client.GetStringAsync("/Business/Dashboard");

        Assert.Contains("pilot-business-blocked", dashboardHtml);
        Assert.Contains("Este negocio no esta activo en Puntelio.", dashboardHtml);
    }

    [Fact]
    public async Task AdminCreateBusiness_WithInviteSendsBusinessPasswordSetupEmail()
    {
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:PublicBaseUrl"] = "https://app.puntelio.com"
        });
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var suffix = NewLegacySafeUserName("ib");
        var businessName = $"Inv {suffix[..8]}";
        var businessEmail = $"{suffix}@biz.test";
        const string password = "StartPass123!";

        await LoginAdminAsync(client);
        var token = await GetAntiforgeryTokenAsync(client, "/Admin/CreateBusiness");
        var response = await client.PostAsync(
            "/Admin/CreateBusiness",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.BusinessName"] = businessName,
                ["Input.BusinessEmail"] = businessEmail,
                ["Input.InitialPassword"] = password,
                ["Input.ConfirmPassword"] = password,
                ["Input.EnablePilot"] = "true",
                ["Input.SendInvite"] = "true",
                ["__RequestVerificationToken"] = token
            }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Invitacion enviada", html);
        Assert.DoesNotContain(password, html, StringComparison.Ordinal);
        Assert.DoesNotContain("/Business/ResetPassword/", html, StringComparison.OrdinalIgnoreCase);

        using var scope = fake.Factory.Services.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IPasswordResetEmailOutbox>();
        var message = Assert.Single(await outbox.ListPasswordResetsAsync());
        Assert.Equal(businessEmail, message.To);
        Assert.Equal(businessName, message.RecipientName);
        Assert.Equal("negocio", message.AccountType);
        Assert.StartsWith("https://app.puntelio.com/Business/ResetPassword/", message.ResetUrl);
    }

    [Fact]
    public async Task AdminBusinessProfile_UpdatesBusinessAndResetsPassword()
    {
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:Pilot:Enabled"] = "true"
        });
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var suffix = NewLegacySafeUserName("pf");
        var businessName = $"Biz {suffix[..8]}";
        var businessEmail = $"{suffix}@biz.test";
        var updatedName = $"Upd {suffix[..8]}";
        var updatedEmail = $"{suffix}@up.test";
        const string oldPassword = "StartPass123!";
        const string newPassword = "ChangedPass123!";

        await LoginAdminAsync(client);
        var createToken = await GetAntiforgeryTokenAsync(client, "/Admin/CreateBusiness");
        await client.PostAsync(
            "/Admin/CreateBusiness",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.BusinessName"] = businessName,
                ["Input.BusinessEmail"] = businessEmail,
                ["Input.InitialPassword"] = oldPassword,
                ["Input.ConfirmPassword"] = oldPassword,
                ["Input.EnablePilot"] = "false",
                ["__RequestVerificationToken"] = createToken
            }));

        Guid businessId;
        using (var scope = fake.Factory.Services.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<InMemoryDigitalCardsStore>();
            businessId = store.Businesses.Single(existing => existing.Email == businessEmail).Id;
        }

        var listHtml = await client.GetStringAsync($"/Admin/Businesses?Query={businessEmail}");
        Assert.Contains("admin-manage-business", listHtml);

        var profilePath = $"/Admin/BusinessProfile/{businessId}";
        var saveToken = await GetAntiforgeryTokenAsync(client, profilePath);
        var saveResponse = await client.PostAsync(
            $"{profilePath}?handler=Save",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.BusinessName"] = updatedName,
                ["Input.BusinessEmail"] = updatedEmail,
                ["Input.BusinessLogo"] = "~/Logos/updated.png",
                ["Input.IsPilotEnabled"] = "true",
                ["Input.ActivationStatus"] = "ModernPrimary",
                ["Input.Notes"] = "actualizado por web test",
                ["__RequestVerificationToken"] = saveToken
            }));
        var saveHtml = await saveResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
        Assert.Contains("Negocio actualizado", saveHtml);
        Assert.Contains(updatedName, saveHtml);
        Assert.Contains("~/Logos/updated.png", saveHtml);
        Assert.Contains("Activo", saveHtml);

        var updatedListHtml = await client.GetStringAsync($"/Admin/Businesses?Query={updatedEmail}");
        Assert.Contains("Activo", updatedListHtml);

        var brandingToken = ExtractAntiforgeryToken(saveHtml);
        var brandingResponse = await client.PostAsync(
            $"{profilePath}?handler=Branding",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["BrandingInput.PublicName"] = "Perfil Publico",
                ["BrandingInput.ProgramName"] = "Perfil Rewards",
                ["BrandingInput.ProgramDescription"] = "Sellos con branding desde web test.",
                ["BrandingInput.StampGoal"] = "12",
                ["BrandingInput.PrimaryColor"] = "#123456",
                ["BrandingInput.SecondaryColor"] = "#abcdef",
                ["BrandingInput.CustomFieldColor"] = "fedcba",
                ["__RequestVerificationToken"] = brandingToken
            }));
        var brandingHtml = await brandingResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, brandingResponse.StatusCode);
        Assert.Contains("Branding del negocio actualizado", brandingHtml);
        Assert.Contains("Perfil Publico", brandingHtml);
        Assert.Contains("#123456", brandingHtml);
        Assert.Contains("#fedcba", brandingHtml);
        Assert.Contains("12", brandingHtml);

        var resetToken = ExtractAntiforgeryToken(brandingHtml);
        var resetResponse = await client.PostAsync(
            $"{profilePath}?handler=ResetPassword",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["PasswordInput.NewPassword"] = newPassword,
                ["PasswordInput.ConfirmPassword"] = newPassword,
                ["__RequestVerificationToken"] = resetToken
            }));
        var resetHtml = await resetResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);
        Assert.Contains("Contrasena de negocio actualizada", resetHtml);
        Assert.DoesNotContain(newPassword, resetHtml, StringComparison.Ordinal);

        var oldLogin = await LoginBusinessAsync(client, updatedEmail, oldPassword);
        Assert.Equal(HttpStatusCode.OK, oldLogin.StatusCode);
        var newLogin = await LoginBusinessAsync(client, updatedEmail, newPassword);
        Assert.Equal(HttpStatusCode.Redirect, newLogin.StatusCode);
        var dashboardHtml = await client.GetStringAsync("/Business/Dashboard");

        Assert.Contains(updatedName, dashboardHtml);
        Assert.DoesNotContain("pilot-business-blocked", dashboardHtml);
    }

    [Fact]
    public async Task AdminBusinessProfile_SendInviteSendsBusinessPasswordSetupEmail()
    {
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:PublicBaseUrl"] = "https://app.puntelio.com"
        });
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginAdminAsync(client);

        var profilePath = "/Admin/BusinessProfile/11111111-1111-1111-1111-111111111111";
        var token = await GetAntiforgeryTokenAsync(client, profilePath);
        var response = await client.PostAsync(
            $"{profilePath}?handler=SendInvite",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token
            }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Invitacion enviada", html);
        Assert.DoesNotContain("/Business/ResetPassword/", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("business123", html, StringComparison.OrdinalIgnoreCase);

        using var scope = fake.Factory.Services.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IPasswordResetEmailOutbox>();
        var message = Assert.Single(await outbox.ListPasswordResetsAsync());
        Assert.Equal("demo@digitalcards.test", message.To);
        Assert.Equal("Demo Coffee", message.RecipientName);
        Assert.Equal("negocio", message.AccountType);
        Assert.StartsWith("https://app.puntelio.com/Business/ResetPassword/", message.ResetUrl);
    }

    [Fact]
    public async Task AdminBusinessProfile_InactiveBusinessCannotLoginModern()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginAdminAsync(client);
        var profilePath = "/Admin/BusinessProfile/11111111-1111-1111-1111-111111111111";
        var token = await GetAntiforgeryTokenAsync(client, profilePath);
        var saveResponse = await client.PostAsync(
            $"{profilePath}?handler=Save",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.BusinessName"] = "Demo Coffee",
                ["Input.BusinessEmail"] = "demo@digitalcards.test",
                ["Input.BusinessLogo"] = "/img/demo-coffee.svg",
                ["Input.IsPilotEnabled"] = "true",
                ["Input.ActivationStatus"] = "Inactive",
                ["Input.Notes"] = "desactivado por admin",
                ["__RequestVerificationToken"] = token
            }));
        var saveHtml = await saveResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
        Assert.Contains("Negocio actualizado", saveHtml);
        Assert.Contains("Inactivo", saveHtml);

        var login = await LoginBusinessAsync(client);
        var loginHtml = await login.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains("inactivo en Puntelio", loginHtml);
        Assert.False(HasBusinessCookie(login));
    }

    [Fact]
    public async Task AdminBusinessProfile_NormalizesLegacyStatusToActiveInactiveLabels()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginAdminAsync(client);
        var profilePath = "/Admin/BusinessProfile/11111111-1111-1111-1111-111111111111";
        var token = await GetAntiforgeryTokenAsync(client, profilePath);
        var saveResponse = await client.PostAsync(
            $"{profilePath}?handler=Save",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.BusinessName"] = "Demo Coffee",
                ["Input.BusinessEmail"] = "demo@digitalcards.test",
                ["Input.BusinessLogo"] = "/img/demo-coffee.svg",
                ["Input.IsPilotEnabled"] = "true",
                ["Input.ActivationStatus"] = "LegacyRetired",
                ["Input.Notes"] = "estado anterior normalizado por test",
                ["__RequestVerificationToken"] = token
            }));
        var saveHtml = await saveResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
        Assert.Contains("Activo", saveHtml);
        Assert.DoesNotContain("Legacy retirado", saveHtml);
        Assert.DoesNotContain("admin-business-legacy-retired-warning", saveHtml);
        Assert.DoesNotContain("ModernPrimary", saveHtml);
        Assert.DoesNotContain("PilotModern", saveHtml);
        Assert.DoesNotContain("LegacyOnly", saveHtml);
        Assert.DoesNotContain("LegacyRetired", saveHtml);

        var supportHtml = await client.GetStringAsync("/Admin/Support?BusinessFilter=Demo");

        Assert.Contains("admin-support-businesses", supportHtml);
        Assert.Contains("Activo", supportHtml);
        Assert.DoesNotContain("admin-support-legacy-retired", supportHtml);
        Assert.DoesNotContain("confirmar bloqueo manual", supportHtml);
    }

    [Fact]
    public async Task AdminBusinessProfile_CanPermanentlyDeleteBusinessCardsAndWalletData()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var suffix = NewLegacySafeUserName("bdl");
        var businessName = $"Del {suffix[..8]}";
        var businessEmail = $"{suffix}@biz.test";
        var userName = NewLegacySafeUserName("cdl");
        const string password = "StartPass123!";

        Guid businessId;
        Guid clientId;
        Guid cardId;
        using (var scope = fake.Factory.Services.CreateScope())
        {
            var adminApp = scope.ServiceProvider.GetRequiredService<AdminAppService>();
            var app = scope.ServiceProvider.GetRequiredService<DigitalCardsAppService>();
            var admin = await adminApp.LoginAdminAsync(new AdminLoginCommand("DCAdmin", "admin123"));
            var created = await adminApp.CreateBusinessAsync(new CreateBusinessCommand(
                businessName,
                businessEmail,
                password,
                admin!.Id,
                EnablePilot: true,
                Notes: "delete web test"));
            var registeredClient = await app.RegisterClientAsync(new RegisterClientCommand(
                userName,
                "Delete",
                "Client",
                $"{userName}@example.test"));
            var enrollment = await app.EnrollClientAsync(new EnrollClientCommand(
                created.Business!.BusinessId,
                registeredClient.UserName,
                "http://localhost"));

            businessId = created.Business.BusinessId;
            clientId = registeredClient.Id;
            cardId = enrollment.Card.Id;
        }

        await LoginAdminAsync(client);
        var profilePath = $"/Admin/BusinessProfile/{businessId}";
        var token = await GetAntiforgeryTokenAsync(client, profilePath);
        var response = await client.PostAsync(
            $"{profilePath}?handler=DeleteBusiness",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["confirmation"] = businessName,
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Admin/Businesses", response.Headers.Location?.OriginalString);

        using var verifyScope = fake.Factory.Services.CreateScope();
        var store = verifyScope.ServiceProvider.GetRequiredService<InMemoryDigitalCardsStore>();
        lock (store.Sync)
        {
            Assert.DoesNotContain(store.Businesses, business => business.Id == businessId);
            Assert.DoesNotContain(store.LoyaltyCards, card => card.Id == cardId);
            Assert.Contains(store.Clients, existingClient => existingClient.Id == clientId);
            Assert.DoesNotContain(store.BusinessCredentials, credential => credential.BusinessId == businessId);
            Assert.DoesNotContain(store.PilotBusinesses, access => access.BusinessId == businessId);
            Assert.DoesNotContain(store.WalletLinkTokens, tokenRecord => tokenRecord.CardId == cardId);
            Assert.Contains(store.AuditEvents, audit =>
                audit.EventType == OperationalAuditEventType.BusinessDeleted &&
                audit.BusinessId == businessId);
        }
    }

    [Fact]
    public async Task AdminBusinessProfile_UploadsBrandingLogoToPublicPath()
    {
        var uploadRoot = Path.Combine(Path.GetTempPath(), $"digitalcards-web-logo-{Guid.NewGuid():N}");
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:Branding:LogoUploads:Path"] = uploadRoot,
            ["DigitalCards:Branding:LogoUploads:RequestPath"] = "/uploads/business-logos"
        });
        try
        {
            var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            await LoginAdminAsync(client);
            var profilePath = "/Admin/BusinessProfile/11111111-1111-1111-1111-111111111111";
            var getHtml = await client.GetStringAsync(profilePath);
        Assert.DoesNotContain("data-testid=\"admin-business-branding-logo\"", getHtml);
        Assert.Contains("data-testid=\"admin-business-branding-logo-preview\"", getHtml);
        Assert.Contains("Nombre del negocio", getHtml);
        Assert.Contains("Numero de sellos", getHtml);
        Assert.Contains("Color secundario 1", getHtml);
        Assert.Contains("Color secundario 2", getHtml);
        var token = ExtractAntiforgeryToken(getHtml);
            using var form = new MultipartFormDataContent
            {
                { new StringContent("Demo Coffee"), "BrandingInput.PublicName" },
                { new StringContent("Demo Rewards"), "BrandingInput.ProgramName" },
                { new StringContent("Logo upload test."), "BrandingInput.ProgramDescription" },
                { new StringContent("10"), "BrandingInput.StampGoal" },
                { new StringContent("#123456"), "BrandingInput.PrimaryColor" },
                { new StringContent("#abcdef"), "BrandingInput.SecondaryColor" },
                { new StringContent(token), "__RequestVerificationToken" }
            };
            var file = new ByteArrayContent(RectangularPng());
            file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(file, "BrandingInput.LogoUpload", "logo.png");

            var response = await client.PostAsync($"{profilePath}?handler=Branding", form);
            var html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Branding del negocio actualizado", html);
            Assert.DoesNotContain(uploadRoot, html);

            string logoPath;
            using (var scope = fake.Factory.Services.CreateScope())
            {
                var store = scope.ServiceProvider.GetRequiredService<InMemoryDigitalCardsStore>();
                logoPath = store.BusinessBranding.Single(
                    branding => branding.BusinessId == Guid.Parse("11111111-1111-1111-1111-111111111111")).LogoPath;
            }

            Assert.StartsWith("/uploads/business-logos/", logoPath, StringComparison.Ordinal);
            Assert.EndsWith("/logo.png", logoPath, StringComparison.Ordinal);
            var logoResponse = await client.GetAsync(logoPath);
            Assert.Equal(HttpStatusCode.OK, logoResponse.StatusCode);
            Assert.Equal((512, 512), ReadPngDimensions(await logoResponse.Content.ReadAsByteArrayAsync()));

            var firstLogoPath = logoPath;
            File.Delete(ToLogoPhysicalPath(uploadRoot, firstLogoPath));
            var secondToken = await GetAntiforgeryTokenAsync(client, profilePath);
            using var secondForm = new MultipartFormDataContent
            {
                { new StringContent("Demo Coffee"), "BrandingInput.PublicName" },
                { new StringContent("Demo Rewards"), "BrandingInput.ProgramName" },
                { new StringContent("Logo replacement test."), "BrandingInput.ProgramDescription" },
                { new StringContent("10"), "BrandingInput.StampGoal" },
                { new StringContent("#123456"), "BrandingInput.PrimaryColor" },
                { new StringContent("#abcdef"), "BrandingInput.SecondaryColor" },
                { new StringContent(secondToken), "__RequestVerificationToken" }
            };
            var secondFile = new ByteArrayContent(RectangularPng());
            secondFile.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            secondForm.Add(secondFile, "BrandingInput.LogoUpload", "logo-2.png");

            var secondResponse = await client.PostAsync($"{profilePath}?handler=Branding", secondForm);

            Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
            using (var scope = fake.Factory.Services.CreateScope())
            {
                var store = scope.ServiceProvider.GetRequiredService<InMemoryDigitalCardsStore>();
                logoPath = store.BusinessBranding.Single(
                    branding => branding.BusinessId == Guid.Parse("11111111-1111-1111-1111-111111111111")).LogoPath;
            }

            Assert.NotEqual(firstLogoPath, logoPath);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(firstLogoPath)).StatusCode);
        }
        finally
        {
            if (Directory.Exists(uploadRoot))
            {
                Directory.Delete(uploadRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AdminBusinessProfile_RejectsInvalidBrandingLogoUpload()
    {
        var uploadRoot = Path.Combine(Path.GetTempPath(), $"digitalcards-web-logo-{Guid.NewGuid():N}");
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:Branding:LogoUploads:Path"] = uploadRoot,
            ["DigitalCards:Branding:LogoUploads:RequestPath"] = "/uploads/business-logos"
        });
        try
        {
            var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            await LoginAdminAsync(client);
            var profilePath = "/Admin/BusinessProfile/11111111-1111-1111-1111-111111111111";
            var token = await GetAntiforgeryTokenAsync(client, profilePath);
            using var form = new MultipartFormDataContent
            {
                { new StringContent("Demo Coffee"), "BrandingInput.PublicName" },
                { new StringContent("Demo Rewards"), "BrandingInput.ProgramName" },
                { new StringContent("Logo upload test."), "BrandingInput.ProgramDescription" },
                { new StringContent("10"), "BrandingInput.StampGoal" },
                { new StringContent("#123456"), "BrandingInput.PrimaryColor" },
                { new StringContent("#abcdef"), "BrandingInput.SecondaryColor" },
                { new StringContent(token), "__RequestVerificationToken" }
            };
            var file = new ByteArrayContent("<svg></svg>"u8.ToArray());
            file.Headers.ContentType = new MediaTypeHeaderValue("image/svg+xml");
            form.Add(file, "BrandingInput.LogoUpload", "logo.svg");

            var response = await client.PostAsync($"{profilePath}?handler=Branding", form);
            var html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("El logo debe ser PNG", html);
            Assert.DoesNotContain("/uploads/business-logos/", html);
        }
        finally
        {
            if (Directory.Exists(uploadRoot))
            {
                Directory.Delete(uploadRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AdminBusinessProfile_RejectsPngExtensionWithNonPngContent()
    {
        var uploadRoot = Path.Combine(Path.GetTempPath(), $"digitalcards-web-logo-{Guid.NewGuid():N}");
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:Branding:LogoUploads:Path"] = uploadRoot,
            ["DigitalCards:Branding:LogoUploads:RequestPath"] = "/uploads/business-logos"
        });
        try
        {
            var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            await LoginAdminAsync(client);
            const string profilePath = "/Admin/BusinessProfile/11111111-1111-1111-1111-111111111111";
            var token = await GetAntiforgeryTokenAsync(client, profilePath);
            using var form = new MultipartFormDataContent
            {
                { new StringContent("Demo Coffee"), "BrandingInput.PublicName" },
                { new StringContent("Demo Rewards"), "BrandingInput.ProgramName" },
                { new StringContent("Logo upload test."), "BrandingInput.ProgramDescription" },
                { new StringContent("10"), "BrandingInput.StampGoal" },
                { new StringContent("#123456"), "BrandingInput.PrimaryColor" },
                { new StringContent("#abcdef"), "BrandingInput.SecondaryColor" },
                { new StringContent(token), "__RequestVerificationToken" }
            };
            var file = new ByteArrayContent("not a png"u8.ToArray());
            file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(file, "BrandingInput.LogoUpload", "logo.png");

            var response = await client.PostAsync($"{profilePath}?handler=Branding", form);
            var html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("El archivo no es un PNG valido", html);
            Assert.DoesNotContain("/uploads/business-logos/", html);
        }
        finally
        {
            if (Directory.Exists(uploadRoot))
            {
                Directory.Delete(uploadRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AdminBusinessProfile_RefreshesWalletBrandingForRecentCards()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var enrollment = await CreateEnrollmentAsync(fake.Factory, NewLegacySafeUserName("abr"));
        using (var scope = fake.Factory.Services.CreateScope())
        {
            var app = scope.ServiceProvider.GetRequiredService<DigitalCardsAppService>();
            var applePasses = scope.ServiceProvider.GetRequiredService<IAppleWalletPassRepository>();
            await app.SelectGoogleWalletAsync(ExtractWalletToken(enrollment.EnrollmentUrl));
            await applePasses.UpsertPassAsync(new AppleWalletPassRecord(
                "pass.com.puntelio.loyalty",
                $"serial-{enrollment.Card.Id:N}",
                enrollment.Card.Id,
                "auth-token-secret-hash",
                "42",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow));
        }

        await LoginAdminAsync(client);
        var profilePath = "/Admin/BusinessProfile/11111111-1111-1111-1111-111111111111";
        var getHtml = await client.GetStringAsync(profilePath);
        var token = ExtractAntiforgeryToken(getHtml);

        Assert.Contains("Actualizar Tarjetas", getHtml);
        Assert.Contains(">Actualizar<", getHtml);
        Assert.Contains("Guardar y actualizar", getHtml);
        Assert.DoesNotContain("Refrescar Wallets recientes", getHtml);
        Assert.DoesNotContain("admin-business-wallet-branding-refresh-limit", getHtml);

        var response = await client.PostAsync(
            $"{profilePath}?handler=RefreshWalletBranding",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token
            }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Actualizacion ejecutada", html);
        Assert.Contains("admin-business-wallet-branding-refresh-result", html);
        Assert.DoesNotContain("auth-token-secret-hash", html, StringComparison.OrdinalIgnoreCase);

        using var verifyScope = fake.Factory.Services.CreateScope();
        var ledger = verifyScope.ServiceProvider.GetRequiredService<IStampLedgerRepository>();
        var record = Assert.Single((await ledger.ListRecentByCardIdAsync(enrollment.Card.Id, 5))
            .Where(item => item.Source == StampLedgerSource.BrandingRefresh));
        Assert.True(record.GoogleWalletSucceeded);
        Assert.True(record.AppleWalletSucceeded);
        Assert.Null(record.ErrorSummary);
    }

    [Fact]
    public async Task AdminBusinessProfile_SaveAndRefreshStoresBrandingAndUpdatesTrackedCards()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var enrollment = await CreateEnrollmentAsync(fake.Factory, NewLegacySafeUserName("asr"));
        using (var scope = fake.Factory.Services.CreateScope())
        {
            var app = scope.ServiceProvider.GetRequiredService<DigitalCardsAppService>();
            await app.SelectGoogleWalletAsync(ExtractWalletToken(enrollment.EnrollmentUrl));
        }

        await LoginAdminAsync(client);
        var profilePath = "/Admin/BusinessProfile/11111111-1111-1111-1111-111111111111";
        var getHtml = await client.GetStringAsync(profilePath);
        var response = await client.PostAsync(
            $"{profilePath}?handler=SaveAndRefresh",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["BrandingInput.PublicName"] = "Admin Cafe Puntelio",
                ["BrandingInput.PrimaryColor"] = "#123456",
                ["BrandingInput.SecondaryColor"] = "#abcdef",
                ["BrandingInput.ProgramName"] = "Admin Programa",
                ["BrandingInput.ProgramDescription"] = "Recompensa admin",
                ["BrandingInput.StampGoal"] = "10",
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(getHtml)
            }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Branding del negocio actualizado. Actualizacion ejecutada", html);
        Assert.Contains("admin-business-wallet-branding-refresh-result", html);
        Assert.Contains("Admin Cafe Puntelio", html);

        using var verifyScope = fake.Factory.Services.CreateScope();
        var ledger = verifyScope.ServiceProvider.GetRequiredService<IStampLedgerRepository>();
        Assert.Contains(
            await ledger.ListRecentByCardIdAsync(enrollment.Card.Id, 5),
            item => item.Source == StampLedgerSource.BrandingRefresh && item.GoogleWalletSucceeded);
    }

    [Fact]
    public async Task AdminCreateAdmin_CreatesAdminWithModernCredentialAndAllowsLogin()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var suffix = NewLegacySafeUserName("ad");
        var userName = $"a{suffix[..8]}";
        var email = $"{suffix}@ad.test";
        const string password = "NewAdmin123!";

        await LoginAdminAsync(client);
        var token = await GetAntiforgeryTokenAsync(client, "/Admin/CreateAdmin");
        var response = await client.PostAsync(
            "/Admin/CreateAdmin",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.UserName"] = userName,
                ["Input.FirstName"] = "New",
                ["Input.LastName"] = "Admin",
                ["Input.Email"] = email,
                ["Input.InitialPassword"] = password,
                ["Input.ConfirmPassword"] = password,
                ["__RequestVerificationToken"] = token
            }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("admin-create-admin-status", html);
        Assert.Contains(userName, html);
        Assert.DoesNotContain(password, html, StringComparison.Ordinal);

        using (var scope = fake.Factory.Services.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<InMemoryDigitalCardsStore>();
            var admin = store.AdminUsers.Single(existing => existing.Email == email);
            Assert.Equal(25, admin.PasswordHashPlaceholder.Length);
            Assert.DoesNotContain(password, admin.PasswordHashPlaceholder, StringComparison.Ordinal);
            Assert.Contains(store.AdminCredentials, credential => credential.AdminUserId == admin.Id);
        }

        var loginClient = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var loginResponse = await LoginAdminAsync(loginClient, userName, password);

        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        Assert.True(HasAdminCookie(loginResponse));
    }

    [Fact]
    public async Task AdminUsers_ResetAdminPasswordInvalidatesOldPassword()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var suffix = NewLegacySafeUserName("ra");
        var userName = $"a{suffix[..8]}";
        var email = $"{suffix}@ad.test";
        const string oldPassword = "NewAdmin123!";
        const string newPassword = "ChangedAdmin123!";

        await LoginAdminAsync(client);
        var createToken = await GetAntiforgeryTokenAsync(client, "/Admin/CreateAdmin");
        await client.PostAsync(
            "/Admin/CreateAdmin",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.UserName"] = userName,
                ["Input.FirstName"] = "Reset",
                ["Input.LastName"] = "Admin",
                ["Input.Email"] = email,
                ["Input.InitialPassword"] = oldPassword,
                ["Input.ConfirmPassword"] = oldPassword,
                ["__RequestVerificationToken"] = createToken
            }));

        Guid adminUserId;
        using (var scope = fake.Factory.Services.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<InMemoryDigitalCardsStore>();
            adminUserId = store.AdminUsers.Single(existing => existing.Email == email).Id;
        }

        var usersHtml = await client.GetStringAsync("/Admin/AdminUsers");
        Assert.Contains("admin-user-row", usersHtml);
        var resetToken = ExtractAntiforgeryToken(usersHtml);
        var resetResponse = await client.PostAsync(
            "/Admin/AdminUsers?handler=ResetPassword",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["targetAdminUserId"] = adminUserId.ToString("D"),
                ["newPassword"] = newPassword,
                ["confirmPassword"] = newPassword,
                ["__RequestVerificationToken"] = resetToken
            }));
        var resetHtml = await resetResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);
        Assert.Contains("Contrasena de admin actualizada", resetHtml);
        Assert.DoesNotContain(newPassword, resetHtml, StringComparison.Ordinal);

        var oldLoginClient = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var oldLogin = await LoginAdminAsync(oldLoginClient, userName, oldPassword);
        var oldLoginHtml = await oldLogin.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, oldLogin.StatusCode);
        Assert.Contains("Credenciales de admin invalidas", oldLoginHtml);

        var newLoginClient = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var newLogin = await LoginAdminAsync(newLoginClient, userName, newPassword);

        Assert.Equal(HttpStatusCode.Redirect, newLogin.StatusCode);
        Assert.True(HasAdminCookie(newLogin));
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

        foreach (var path in new[] { "/Business/Dashboard", "/Business/Enroll", "/Business/Stamp", "/Business/Cards", "/Business/Branding" })
        {
            var response = await client.GetAsync(path);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var checkInResponse = await client.GetAsync("/Business/CheckIn");
        Assert.Equal(HttpStatusCode.Redirect, checkInResponse.StatusCode);
        Assert.Contains("/Business/Cards", checkInResponse.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task BusinessEnrollAndStamp_UseLegacyParityPanels()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginBusinessAsync(client);

        var enrollHtml = await client.GetStringAsync("/Business/Enroll");
        var stampHtml = await client.GetStringAsync("/Business/Stamp");

        Assert.Contains("business-enroll-panel", enrollHtml);
        Assert.Contains("business-form-card", enrollHtml);
        Assert.Contains("business-stamp-panel", stampHtml);
        Assert.Contains("business-form-card", stampHtml);
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
        Assert.Contains("data-testid=\"current-stamps\">2 de 10</strong>", stampHtml);
        Assert.Contains("data-testid=\"stamp-username\"", stampHtml);
        Assert.DoesNotContain($"value=\"{userName}\"", stampHtml, StringComparison.OrdinalIgnoreCase);
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
    public async Task ClientPages_WithoutCookie_RedirectToLogin()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        foreach (var path in new[] { "/Client/Dashboard", "/Client/Cards", "/Client/Profile", "/Client/ChangePassword" })
        {
            var response = await client.GetAsync(path);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Client/Login", response.Headers.Location?.OriginalString);
        }
    }

    [Fact]
    public async Task ClientLogin_WithValidCredentials_EmitsClientCookieAndRedirects()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var userName = NewLegacySafeUserName("cl");
        const string password = "clientpass1";
        await RegisterClientAsync(fake.Factory, userName, $"{userName}@example.test", password);

        var response = await LoginClientAsync(client, userName, password);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Client/Dashboard", response.Headers.Location?.OriginalString);
        Assert.True(HasClientCookie(response));
        Assert.False(HasBusinessCookie(response));
        Assert.False(HasAdminCookie(response));
    }

    [Fact]
    public async Task ClientLogin_WithInvalidOrBusinessCredentials_DoesNotEmitClientCookie()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var businessLogin = await LoginClientAsync(client, "demo@digitalcards.test", "business123");
        var businessHtml = await businessLogin.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, businessLogin.StatusCode);
        Assert.Contains("Credenciales de cliente invalidas", businessHtml);
        Assert.False(HasClientCookie(businessLogin));
    }

    [Fact]
    public async Task ClientCards_WithCookieShowsOnlyAuthenticatedClientCards()
    {
        using var fake = WithFakeIntegrations();
        var http = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var firstUserName = NewLegacySafeUserName("c1");
        var secondUserName = NewLegacySafeUserName("c2");
        const string password = "clientpass1";
        await CreateEnrollmentAsync(fake.Factory, firstUserName, $"{firstUserName}@example.test", password);
        await CreateEnrollmentAsync(fake.Factory, secondUserName, $"{secondUserName}@example.test", password);

        var login = await LoginClientAsync(http, firstUserName, password);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        var html = await http.GetStringAsync($"/Client/Cards?UserName={secondUserName}");
        var css = await http.GetStringAsync("/css/site.css");

        Assert.Contains("client-card-results", html);
        Assert.Contains(firstUserName, html);
        Assert.Contains("Demo Coffee", html);
        Assert.DoesNotContain(secondUserName, html);
        Assert.DoesNotContain("client-cards-username", html);
        Assert.Contains("client-card-wallet-status", html);
        Assert.DoesNotContain("client-card-google-status", html);
        Assert.DoesNotContain("client-card-apple-status", html);
        Assert.Contains("client-card-wallet-link", html);
        Assert.Contains("client-cards-qr-card", html);
        Assert.Contains("client-cards-summary", html);
        Assert.Contains("client-card-progress-panel", html);
        Assert.Contains("<svg", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("viewBox=", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("preserveAspectRatio=\"xMidYMid meet\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Sellos historicos", html);
        Assert.DoesNotContain("client-card-lifetime-stamps", html);
        Assert.Contains(".client-qr-card", css);
        Assert.Contains("aspect-ratio: 1;", css);
        Assert.Contains(".client-qr-card svg", css);
        Assert.Contains("max-height: 100%;", css);
        Assert.Contains("height: 100%;", css);
        Assert.Contains("width: 100%;", css);
    }

    [Fact]
    public async Task ClientDashboard_ShowsProfileSummaryAndWalletPreview()
    {
        using var fake = WithFakeIntegrations();
        var http = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var userName = NewLegacySafeUserName("ds");
        const string password = "clientpass1";
        await CreateEnrollmentAsync(fake.Factory, userName, $"{userName}@example.test", password);

        var login = await LoginClientAsync(http, userName, password);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        var html = await http.GetStringAsync("/Client/Dashboard");
        var css = await http.GetStringAsync("/css/site.css");

        Assert.Contains("client-dashboard-summary", html);
        Assert.Contains("client-profile-summary", html);
        Assert.Contains(userName, html);
        Assert.Contains($"{userName}@example.test", html);
        Assert.Contains("client-dashboard-card-count", html);
        Assert.Contains("client-dashboard-current-stamps", html);
        Assert.DoesNotContain("client-dashboard-lifetime-stamps", html);
        Assert.DoesNotContain("Sellos historicos", html);
        Assert.DoesNotContain("historicos", html);
        Assert.Contains("client-dashboard-wallet-link", html);
        Assert.DoesNotContain("client-action-grid", html);
        Assert.DoesNotContain("client-dashboard-cards-link", html);
        Assert.DoesNotContain("client-dashboard-profile-link", html);
        Assert.DoesNotContain("client-logout-link", html);
        Assert.Contains("client-sidebar-dashboard-link", html);
        Assert.Contains("client-sidebar-cards-link", html);
        Assert.Contains("client-sidebar-profile-link", html);
        Assert.Contains("client-layout-logout-link", html);
        Assert.DoesNotContain("client-dashboard-change-password-link", html);
        Assert.DoesNotContain("client-dashboard-google-count", html);
        Assert.DoesNotContain("client-dashboard-apple-count", html);
        Assert.Contains("client-qr-card", html);
        Assert.Contains("<svg", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("viewBox=", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("preserveAspectRatio=\"xMidYMid meet\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("box-sizing: border-box;", css);
        Assert.Contains("overflow: hidden;", css);
        Assert.Contains("width: clamp(96px, 13vw, 112px);", css);
        Assert.Contains(".client-cards-qr-panel .client-qr-card", css);
        Assert.DoesNotContain("00000000-0000-0000", html);
    }

    [Fact]
    public async Task ClientProfile_UpdatesLegacyProfileWithoutChangingUserNameOrRenderingSecrets()
    {
        using var fake = WithFakeIntegrations();
        var http = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var userName = NewLegacySafeUserName("pf");
        const string password = "clientpass1";
        await RegisterClientAsync(fake.Factory, userName, $"{userName}@example.test", password);
        await LoginClientAsync(http, userName, password);
        var getHtml = await http.GetStringAsync("/Client/Profile");

        Assert.Contains("client-profile-form", getHtml);
        Assert.Contains(userName, getHtml);

        var token = ExtractAntiforgeryToken(getHtml);
        var response = await http.PostAsync(
            "/Client/Profile",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.FirstName"] = "Nuevo",
                ["Input.LastName"] = "Cliente",
                ["Input.Email"] = $"{userName}@new.test",
                ["__RequestVerificationToken"] = token
            }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Perfil actualizado", html);
        Assert.Contains("Nuevo", html);
        Assert.Contains($"{userName}@new.test", html);
        Assert.Contains(userName, html);
        Assert.DoesNotContain(password, html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", html, StringComparison.OrdinalIgnoreCase);

        var dashboard = await http.GetStringAsync("/Client/Dashboard");
        Assert.Contains("Nuevo Cliente", dashboard);
        Assert.Contains($"{userName}@new.test", dashboard);
        Assert.Contains(userName, dashboard);
    }

    [Fact]
    public async Task ClientChangePassword_UpdatesPasswordWithoutRenderingSecret()
    {
        using var fake = WithFakeIntegrations();
        var http = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var userName = NewLegacySafeUserName("cp");
        const string oldPassword = "clientpass1";
        const string newPassword = "changedpass1";
        await RegisterClientAsync(fake.Factory, userName, $"{userName}@example.test", oldPassword);
        await LoginClientAsync(http, userName, oldPassword);

        var change = await ChangeClientPasswordAsync(http, oldPassword, newPassword);
        var html = await change.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        Assert.Contains("Contrasena de cliente actualizada", html);
        Assert.DoesNotContain(newPassword, html, StringComparison.Ordinal);

        var logout = await http.GetAsync("/Client/Logout");
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);

        var oldLogin = await LoginClientAsync(http, userName, oldPassword);
        Assert.Equal(HttpStatusCode.OK, oldLogin.StatusCode);
        Assert.False(HasClientCookie(oldLogin));

        var newLogin = await LoginClientAsync(http, userName, newPassword);
        Assert.Equal(HttpStatusCode.Redirect, newLogin.StatusCode);
        Assert.True(HasClientCookie(newLogin));
    }

    [Fact]
    public async Task ClientPasswordReset_UsesFakeOutboxAndDoesNotRenderSecret()
    {
        using var fake = WithFakeIntegrations();
        var http = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var userName = NewLegacySafeUserName("cr");
        const string oldPassword = "clientpass1";
        const string newPassword = "resetpass1";
        await RegisterClientAsync(fake.Factory, userName, $"{userName}@example.test", oldPassword);

        var forgotToken = await GetAntiforgeryTokenAsync(http, "/Client/ForgotPassword");
        var forgot = await http.PostAsync(
            "/Client/ForgotPassword",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.UserNameOrEmail"] = userName,
                ["__RequestVerificationToken"] = forgotToken
            }));
        var forgotHtml = await forgot.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, forgot.StatusCode);
        Assert.Contains("Si existe una cuenta", forgotHtml);

        string resetPath;
        using (var scope = fake.Factory.Services.CreateScope())
        {
            var outbox = scope.ServiceProvider.GetRequiredService<IPasswordResetEmailOutbox>();
            var message = Assert.Single(await outbox.ListPasswordResetsAsync());
            Assert.StartsWith("http://localhost/Client/ResetPassword/", message.ResetUrl);
            resetPath = new Uri(message.ResetUrl).PathAndQuery;
        }

        var resetToken = await GetAntiforgeryTokenAsync(http, resetPath);
        var reset = await http.PostAsync(
            resetPath,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.NewPassword"] = newPassword,
                ["Input.ConfirmPassword"] = newPassword,
                ["Token"] = resetPath.Split('/').Last(),
                ["__RequestVerificationToken"] = resetToken
            }));
        var resetHtml = await reset.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.Contains("Contrasena de cliente actualizada", resetHtml);
        Assert.DoesNotContain(newPassword, resetHtml, StringComparison.Ordinal);

        var oldLogin = await LoginClientAsync(http, userName, oldPassword);
        Assert.Equal(HttpStatusCode.OK, oldLogin.StatusCode);
        Assert.False(HasClientCookie(oldLogin));

        var newLogin = await LoginClientAsync(http, userName, newPassword);
        Assert.Equal(HttpStatusCode.Redirect, newLogin.StatusCode);
        Assert.True(HasClientCookie(newLogin));
    }

    [Fact]
    public async Task BusinessPasswordReset_UsesFakeOutboxAndUpdatesLogin()
    {
        using var fake = WithFakeIntegrations();
        var http = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        const string oldPassword = "business123";
        const string newPassword = "resetbiz123";

        var forgotToken = await GetAntiforgeryTokenAsync(http, "/Business/ForgotPassword");
        var forgot = await http.PostAsync(
            "/Business/ForgotPassword",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.Email"] = "demo@digitalcards.test",
                ["__RequestVerificationToken"] = forgotToken
            }));
        var forgotHtml = await forgot.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, forgot.StatusCode);
        Assert.Contains("Si existe una cuenta", forgotHtml);

        string resetPath;
        using (var scope = fake.Factory.Services.CreateScope())
        {
            var outbox = scope.ServiceProvider.GetRequiredService<IPasswordResetEmailOutbox>();
            var message = Assert.Single(await outbox.ListPasswordResetsAsync());
            Assert.StartsWith("http://localhost/Business/ResetPassword/", message.ResetUrl);
            resetPath = new Uri(message.ResetUrl).PathAndQuery;
        }

        var resetToken = await GetAntiforgeryTokenAsync(http, resetPath);
        var reset = await http.PostAsync(
            resetPath,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.NewPassword"] = newPassword,
                ["Input.ConfirmPassword"] = newPassword,
                ["Token"] = resetPath.Split('/').Last(),
                ["__RequestVerificationToken"] = resetToken
            }));
        var resetHtml = await reset.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.Contains("Contrasena de negocio actualizada", resetHtml);
        Assert.DoesNotContain(newPassword, resetHtml, StringComparison.Ordinal);

        var oldLogin = await LoginBusinessAsync(http, "demo@digitalcards.test", oldPassword);
        Assert.Equal(HttpStatusCode.OK, oldLogin.StatusCode);
        Assert.False(HasBusinessCookie(oldLogin));

        var newLogin = await LoginBusinessAsync(http, "demo@digitalcards.test", newPassword);
        Assert.Equal(HttpStatusCode.Redirect, newLogin.StatusCode);
        Assert.True(HasBusinessCookie(newLogin));
    }

    [Fact]
    public async Task ClientLogout_ClearsCookieAndRedirectsToLogin()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var userName = NewLegacySafeUserName("co");
        const string password = "clientpass1";
        await RegisterClientAsync(fake.Factory, userName, $"{userName}@example.test", password);
        await LoginClientAsync(client, userName, password);

        var response = await client.GetAsync("/Client/Logout");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Client/Login", response.Headers.Location?.OriginalString);
        Assert.True(HasExpiredClientCookie(response));
    }

    [Fact]
    public async Task Pilot_WithAllowedBusinessEmail_AllowsBusinessPages()
    {
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:Pilot:Enabled"] = "true",
            ["DigitalCards:Pilot:AllowedBusinessEmails:0"] = "demo@digitalcards.test"
        });
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginBusinessAsync(client);

        foreach (var path in new[] { "/Business/Dashboard", "/Business/Enroll", "/Business/Stamp", "/Business/Cards", "/Business/Branding" })
        {
            var response = await client.GetAsync(path);
            var html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotContain("pilot-business-blocked", html);
        }
    }

    [Fact]
    public async Task Pilot_WithAllowedBusinessId_AllowsDashboard()
    {
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:Pilot:Enabled"] = "true",
            ["DigitalCards:Pilot:AllowedBusinessIds:0"] = "11111111-1111-1111-1111-111111111111"
        });
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginBusinessAsync(client);

        var html = await client.GetStringAsync("/Business/Dashboard");

        Assert.Contains("data-testid=\"enroll-link\"", html);
        Assert.DoesNotContain("pilot-business-blocked", html);
    }

    [Fact]
    public async Task BusinessDashboard_ShowsOperationalMetricsRecentCardsAndLedger()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var userName = NewLegacySafeUserName("bd");
        var enrollment = await CreateEnrollmentAsync(fake.Factory, userName);
        using (var scope = fake.Factory.Services.CreateScope())
        {
            var app = scope.ServiceProvider.GetRequiredService<DigitalCardsAppService>();
            var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
                "demo@digitalcards.test",
                "business123"));
            await app.SelectGoogleWalletAsync(ExtractWalletToken(enrollment.EnrollmentUrl));
            await app.AddStampToCardAsync(business!.Id, enrollment.Card.Id);
        }

        await LoginBusinessAsync(client);
        var html = await client.GetStringAsync("/Business/Dashboard");

        Assert.Contains("business-dashboard-shell", html);
        Assert.Contains("business-flow-steps", html);
        Assert.Contains("business-dashboard-summary", html);
        Assert.Contains("business-dashboard-card-count", html);
        Assert.Contains("business-dashboard-current-stamps", html);
        Assert.DoesNotContain("business-dashboard-google-count", html);
        Assert.DoesNotContain("business-dashboard-apple-count", html);
        Assert.DoesNotContain("business-dashboard-wallet-issues", html);
        Assert.DoesNotContain("business-reports-link", html);
        Assert.Contains("business-dashboard-recent-card", html);
        Assert.Contains("business-dashboard-ledger-event", html);
        Assert.Contains(userName, html);
        Assert.Contains("Actualizado", html);
        Assert.DoesNotContain("LegacySync", html);
        Assert.DoesNotContain("Google:", html);
        Assert.DoesNotContain("Apple:", html);
        Assert.DoesNotContain("businessId", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("authorization", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("push token", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BusinessReports_ShowsBusinessScopedReportingWithoutSecrets()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var userName = NewLegacySafeUserName("br");
        var enrollment = await CreateEnrollmentAsync(fake.Factory, userName);
        using (var scope = fake.Factory.Services.CreateScope())
        {
            var app = scope.ServiceProvider.GetRequiredService<DigitalCardsAppService>();
            var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
                "demo@digitalcards.test",
                "business123"));
            await app.SelectGoogleWalletAsync(ExtractWalletToken(enrollment.EnrollmentUrl));
            await app.AddStampToCardAsync(business!.Id, enrollment.Card.Id);
        }

        await LoginBusinessAsync(client);
        var html = await client.GetStringAsync("/Business/Reports");

        Assert.Contains("business-reports", html);
        Assert.Contains("business-report-card-count", html);
        Assert.Contains("business-report-client-count", html);
        Assert.DoesNotContain("business-report-wallet-ready-count", html);
        Assert.DoesNotContain("business-report-wallet-issues", html);
        Assert.DoesNotContain("Errores Wallet recientes", html);
        Assert.Contains("business-report-client-breakdown", html);
        Assert.Contains("business-report-period", html);
        Assert.Contains("business-report-recent-client", html);
        Assert.Contains(userName, html);
        Assert.DoesNotContain(enrollment.Card.EnrollmentToken, html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("business123", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("push-token", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("auth-token", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BusinessBranding_AllowsBusinessToUpdatePublicBrandingAndUploadLogo()
    {
        var uploadRoot = Path.Combine(Path.GetTempPath(), $"digitalcards-business-logo-{Guid.NewGuid():N}");
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:Branding:LogoUploads:Path"] = uploadRoot,
            ["DigitalCards:Branding:LogoUploads:RequestPath"] = "/uploads/business-logos"
        });
        try
        {
            var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            await LoginBusinessAsync(client);
            var getHtml = await client.GetStringAsync("/Business/Branding");

            Assert.Contains("business-branding-form", getHtml);
            Assert.Contains("Demo Coffee", getHtml);
            Assert.DoesNotContain("data-testid=\"business-branding-logo\"", getHtml);

            var token = ExtractAntiforgeryToken(getHtml);
            using var form = new MultipartFormDataContent
            {
                { new StringContent("Self Managed Coffee"), "Input.PublicName" },
                { new StringContent("Self Rewards"), "Input.ProgramName" },
                { new StringContent("Branding editado por negocio."), "Input.ProgramDescription" },
                { new StringContent("14"), "Input.StampGoal" },
                { new StringContent("#102030"), "Input.PrimaryColor" },
                { new StringContent("#405060"), "Input.SecondaryColor" },
                { new StringContent("778899"), "Input.CustomFieldColor" },
                { new StringContent(token), "__RequestVerificationToken" }
            };
            var file = new ByteArrayContent(RectangularPng());
            file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(file, "Input.LogoUpload", "logo.png");

            var response = await client.PostAsync("/Business/Branding", form);
            var html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Branding actualizado", html);
            Assert.Contains("Self Managed Coffee", html);
            Assert.Contains("#102030", html);
            Assert.DoesNotContain(uploadRoot, html);
            Assert.DoesNotContain("password", html, StringComparison.OrdinalIgnoreCase);

            string logoPath;
            using (var scope = fake.Factory.Services.CreateScope())
            {
                var store = scope.ServiceProvider.GetRequiredService<InMemoryDigitalCardsStore>();
                var branding = Assert.Single(store.BusinessBranding);
                logoPath = branding.LogoPath;
                Assert.Equal("Self Rewards", branding.ProgramName);
                Assert.Equal("#778899", branding.CustomFieldColor);
                Assert.Equal(14, branding.StampGoal);
            }

            Assert.StartsWith("/uploads/business-logos/", logoPath, StringComparison.Ordinal);
            Assert.EndsWith("/logo.png", logoPath, StringComparison.Ordinal);
            var logoResponse = await client.GetAsync(logoPath);
            Assert.Equal(HttpStatusCode.OK, logoResponse.StatusCode);
            Assert.Equal((512, 512), ReadPngDimensions(await logoResponse.Content.ReadAsByteArrayAsync()));
        }
        finally
        {
            if (Directory.Exists(uploadRoot))
            {
                Directory.Delete(uploadRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BusinessBranding_CanRefreshWalletBrandingForRecentCards()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var enrollment = await CreateEnrollmentAsync(fake.Factory, NewLegacySafeUserName("bbr"));
        using (var scope = fake.Factory.Services.CreateScope())
        {
            var app = scope.ServiceProvider.GetRequiredService<DigitalCardsAppService>();
            var applePasses = scope.ServiceProvider.GetRequiredService<IAppleWalletPassRepository>();
            await app.SelectGoogleWalletAsync(ExtractWalletToken(enrollment.EnrollmentUrl));
            await applePasses.UpsertPassAsync(new AppleWalletPassRecord(
                "pass.com.puntelio.loyalty",
                $"serial-{enrollment.Card.Id:N}",
                enrollment.Card.Id,
                "auth-token-secret-hash",
                "42",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow));
        }

        await LoginBusinessAsync(client);
        var getHtml = await client.GetStringAsync("/Business/Branding");
        var token = ExtractAntiforgeryToken(getHtml);

        Assert.Contains("Recompensa", getHtml);
        Assert.DoesNotContain(">Descripcion<", getHtml);
        Assert.Contains("business-branding-description", getHtml);
        Assert.Contains("business-branding-stamp-goal", getHtml);
        Assert.Contains("Nombre del negocio", getHtml);
        Assert.Contains("Numero de sellos", getHtml);
        Assert.Contains("Color secundario 1", getHtml);
        Assert.Contains("Color secundario 2", getHtml);
        Assert.Contains("business-branding-custom-field-color", getHtml);
        Assert.Contains("type=\"text\"", getHtml);
        Assert.DoesNotContain("<textarea", getHtml);
        Assert.DoesNotContain("business-wallet-branding-refresh-limit", getHtml);
        Assert.Contains(">Guardar<", getHtml);
        Assert.Contains("Guardar y actualizar", getHtml);
        Assert.Contains("Actualizar Tarjetas", getHtml);
        Assert.Contains(">Actualizar<", getHtml);
        Assert.DoesNotContain("Refrescar Wallets recientes", getHtml);

        var response = await client.PostAsync(
            "/Business/Branding?handler=RefreshWallets",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token
            }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Actualizacion ejecutada", html);
        Assert.Contains("business-wallet-branding-refresh-result", html);
        Assert.DoesNotContain("auth-token-secret-hash", html, StringComparison.OrdinalIgnoreCase);

        using var verifyScope = fake.Factory.Services.CreateScope();
        var ledger = verifyScope.ServiceProvider.GetRequiredService<IStampLedgerRepository>();
        var record = Assert.Single((await ledger.ListRecentByCardIdAsync(enrollment.Card.Id, 5))
            .Where(item => item.Source == StampLedgerSource.BrandingRefresh));
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), record.ActorBusinessId);
        Assert.True(record.GoogleWalletSucceeded);
        Assert.True(record.AppleWalletSucceeded);
    }

    [Fact]
    public async Task BusinessBranding_SaveAndRefreshStoresBrandingAndUpdatesTrackedCards()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var enrollment = await CreateEnrollmentAsync(fake.Factory, NewLegacySafeUserName("bsr"));
        using (var scope = fake.Factory.Services.CreateScope())
        {
            var app = scope.ServiceProvider.GetRequiredService<DigitalCardsAppService>();
            await app.SelectGoogleWalletAsync(ExtractWalletToken(enrollment.EnrollmentUrl));
        }

        await LoginBusinessAsync(client);
        var getHtml = await client.GetStringAsync("/Business/Branding");
        var response = await client.PostAsync(
            "/Business/Branding?handler=SaveAndRefresh",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.PublicName"] = "Cafe Puntelio",
                ["Input.PrimaryColor"] = "#123456",
                ["Input.SecondaryColor"] = "#abcdef",
                ["Input.CustomFieldColor"] = "112233",
                ["Input.StampGoal"] = "10",
                ["Input.ProgramName"] = "Programa Puntelio",
                ["Input.ProgramDescription"] = "Recompensa de prueba",
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(getHtml)
            }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Branding actualizado. Actualizacion ejecutada", html);
        Assert.Contains("business-wallet-branding-refresh-result", html);
        Assert.Contains("Cafe Puntelio", html);

        using var verifyScope = fake.Factory.Services.CreateScope();
        var ledger = verifyScope.ServiceProvider.GetRequiredService<IStampLedgerRepository>();
        Assert.Contains(
            await ledger.ListRecentByCardIdAsync(enrollment.Card.Id, 5),
            item => item.Source == StampLedgerSource.BrandingRefresh && item.GoogleWalletSucceeded);
    }

    [Fact]
    public async Task Pilot_WithBlockedBusiness_ShowsMessageAndHidesModernActions()
    {
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:Pilot:Enabled"] = "true",
            ["DigitalCards:Pilot:AllowedBusinessEmails:0"] = "other@digitalcards.test"
        });
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginBusinessAsync(client);

        var html = await client.GetStringAsync("/Business/Dashboard");

        Assert.Contains("pilot-business-blocked", html);
        Assert.Contains("Este negocio no esta activo en Puntelio.", html);
        Assert.DoesNotContain("business-dashboard-summary", html);
        Assert.DoesNotContain("business-public-enrollment-panel", html);

        foreach (var path in new[] { "/Business/Enroll", "/Business/Stamp", "/Business/Cards", "/Business/Branding" })
        {
            var blockedPage = await client.GetStringAsync(path);

            Assert.Contains("pilot-business-blocked", blockedPage);
            Assert.DoesNotContain("data-testid=\"enroll-form\"", blockedPage);
            Assert.DoesNotContain("data-testid=\"stamp-form\"", blockedPage);
            Assert.DoesNotContain("data-testid=\"business-card-search-form\"", blockedPage);
        }
    }

    [Fact]
    public async Task BusinessCheckIn_RedirectsToCards()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginBusinessAsync(client);

        var getResponse = await client.GetAsync("/Business/CheckIn");

        Assert.Equal(HttpStatusCode.Redirect, getResponse.StatusCode);
        Assert.Contains("/Business/Cards", getResponse.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task BusinessCards_SearchShowsOnlyAuthenticatedBusinessCards()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginBusinessAsync(client);
        var userName = NewLegacySafeUserName("bc");
        var enrollment = await CreateEnrollmentAsync(fake.Factory, userName);
        SeedOtherBusinessCard(fake.Factory, "othercard1");

        var html = await client.GetStringAsync($"/Business/Cards?Query={userName}&CardId={enrollment.Card.Id}");

        Assert.Contains("business-card-results", html);
        Assert.Contains(userName, html);
        Assert.Contains(enrollment.Card.Id.ToString(), html);
        Assert.Contains("business-qr-scanner", html);
        Assert.DoesNotContain("business-card-quick-summary", html);
        Assert.DoesNotContain(">Seleccionada<", html);
        Assert.Contains("business-card-action-strip", html);
        Assert.Contains("business-card-result-state", html);
        Assert.Contains("business-detail-card-face", html);
        Assert.Contains("business-wallet-status-row", html);
        Assert.Contains("business-card-wallet-status", html);
        Assert.DoesNotContain("business-card-google-status", html);
        Assert.DoesNotContain("business-card-apple-status", html);
        Assert.DoesNotContain("business-card-apple-devices", html);
        Assert.DoesNotContain("business-card-manage-link", html);
        Assert.DoesNotContain("othercard1", html);
        Assert.DoesNotContain("businessId", html, StringComparison.OrdinalIgnoreCase);
        Assert.True(html.IndexOf("stamp-ledger-list", StringComparison.Ordinal) < html.IndexOf("business-card-management-panel", StringComparison.Ordinal));
        Assert.True(html.IndexOf("business-card-deactivate-submit", StringComparison.Ordinal) < html.IndexOf("business-card-delete-submit", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BusinessCards_CanDeactivateReactivateAndDeleteOwnCardOnly()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginBusinessAsync(client);
        var userName = NewLegacySafeUserName("lc");
        var enrollment = await CreateEnrollmentAsync(fake.Factory, userName);
        var walletToken = ExtractWalletToken(enrollment.EnrollmentUrl);

        var detailPath = $"/Business/Cards?Query={userName}&CardId={enrollment.Card.Id}";
        var detailHtml = await client.GetStringAsync(detailPath);
        var deactivateResponse = await client.PostAsync(
            $"/Business/Cards?handler=Deactivate&cardId={enrollment.Card.Id}&query={userName}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(detailHtml)
            }));
        var inactiveHtml = await deactivateResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        Assert.Contains("Tarjeta desactivada", inactiveHtml);
        Assert.Contains("Inactiva", inactiveHtml);
        Assert.Contains("business-card-reactivate-submit", inactiveHtml);

        var blockedStamp = await client.PostAsync(
            $"/Business/Cards?handler=Stamp&cardId={enrollment.Card.Id}&query={userName}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(inactiveHtml)
            }));
        var blockedStampHtml = await blockedStamp.Content.ReadAsStringAsync();
        Assert.Contains("La tarjeta esta desactivada para este negocio.", blockedStampHtml);
        Assert.Equal(1, FindInMemoryCard(fake.Factory, enrollment.Card.Id).CurrentStamps);

        var reactivateResponse = await client.PostAsync(
            $"/Business/Cards?handler=Reactivate&cardId={enrollment.Card.Id}&query={userName}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(blockedStampHtml)
            }));
        var activeHtml = await reactivateResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, reactivateResponse.StatusCode);
        Assert.Contains("Tarjeta reactivada", activeHtml);
        Assert.Contains("Activa", activeHtml);
        var activeToken = ExtractAntiforgeryToken(activeHtml);

        var otherCardId = SeedOtherBusinessCard(fake.Factory, "otherdel1");
        var wrongDeleteResponse = await client.PostAsync(
            $"/Business/Cards?handler=Delete&cardId={otherCardId}&query=otherdel1",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["confirmation"] = "otherdel1",
                ["__RequestVerificationToken"] = activeToken
            }));
        var wrongDeleteHtml = await wrongDeleteResponse.Content.ReadAsStringAsync();
        Assert.Contains("La tarjeta no existe para este negocio.", wrongDeleteHtml);
        Assert.NotNull(FindInMemoryCardOrNull(fake.Factory, otherCardId));

        var deleteResponse = await client.PostAsync(
            $"/Business/Cards?handler=Delete&cardId={enrollment.Card.Id}&query={userName}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["confirmation"] = userName,
                ["__RequestVerificationToken"] = activeToken
            }));
        var deletedHtml = await deleteResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        Assert.Contains("Tarjeta eliminada", deletedHtml);
        Assert.Null(FindInMemoryCardOrNull(fake.Factory, enrollment.Card.Id));

        using (var verifyScope = fake.Factory.Services.CreateScope())
        {
            var store = verifyScope.ServiceProvider.GetRequiredService<InMemoryDigitalCardsStore>();
            lock (store.Sync)
            {
                Assert.Contains(store.Clients, existingClient => existingClient.UserName == userName);
                Assert.DoesNotContain(store.WalletLinkTokens, token => token.CardId == enrollment.Card.Id);
            }
        }

        var walletHtml = await client.GetStringAsync($"/Wallet/Select/{walletToken}");
        Assert.Contains("wallet-not-found", walletHtml);
    }

    [Fact]
    public async Task BusinessCards_RejectsCardFromAnotherBusiness()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginBusinessAsync(client);
        var otherCardId = SeedOtherBusinessCard(fake.Factory, "othercard2");

        var html = await client.GetStringAsync($"/Business/Cards?Query=othercard2&CardId={otherCardId}");

        Assert.Contains("No se encontraron tarjetas para este negocio.", html);
        Assert.Contains("La tarjeta no existe para este negocio.", html);
        Assert.DoesNotContain("business-card-stamp-submit", html);
    }

    [Fact]
    public async Task BusinessCards_StampByCardIdValidatesBusinessOwner()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginBusinessAsync(client);
        var otherCardId = SeedOtherBusinessCard(fake.Factory, "othercard3");
        var token = await GetAntiforgeryTokenAsync(client, "/Business/Enroll");

        var response = await client.PostAsync(
            $"/Business/Cards?handler=Stamp&cardId={otherCardId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token
            }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("La tarjeta no existe para este negocio.", html);
        Assert.Equal(1, FindInMemoryCard(fake.Factory, otherCardId).CurrentStamps);
    }

    [Fact]
    public async Task BusinessCards_ResendEmailUsesConfiguredPublicBaseUrl()
    {
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:PublicBaseUrl"] = "https://app.puntelio.com"
        });
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginBusinessAsync(client);
        var userName = NewLegacySafeUserName("br");
        var enrollment = await CreateEnrollmentAsync(fake.Factory, userName);
        var token = await GetAntiforgeryTokenAsync(client, $"/Business/Cards?Query={userName}&CardId={enrollment.Card.Id}");

        var response = await client.PostAsync(
            $"/Business/Cards?handler=Resend&cardId={enrollment.Card.Id}&query={userName}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token
            }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Correo reenviado", html);
        Assert.Contains("https://app.puntelio.com/Wallet/Select/", html);
        Assert.DoesNotContain(enrollment.Card.EnrollmentToken, html, StringComparison.OrdinalIgnoreCase);

        using var scope = fake.Factory.Services.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IWalletEmailOutbox>();
        var messages = await outbox.ListAsync();
        Assert.StartsWith("https://app.puntelio.com/Wallet/Select/", messages[0].EnrollmentUrl);
        Assert.DoesNotContain(enrollment.Card.EnrollmentToken, messages[0].EnrollmentUrl, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(enrollment.Card.EnrollmentToken, ExtractWalletToken(messages[0].EnrollmentUrl));
    }

    [Fact]
    public async Task BusinessCards_ShowsStampLedgerAfterModernStamp()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginBusinessAsync(client);
        var userName = NewLegacySafeUserName("sl");
        var enrollment = await CreateEnrollmentAsync(fake.Factory, userName);
        var token = await GetAntiforgeryTokenAsync(
            client,
            $"/Business/Cards?Query={userName}&CardId={enrollment.Card.Id}");

        var response = await client.PostAsync(
            $"/Business/Cards?handler=Stamp&cardId={enrollment.Card.Id}&query={userName}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token
            }));
        var html = await response.Content.ReadAsStringAsync();
        var text = Regex.Replace(WebUtility.HtmlDecode(html), "\\s+", " ");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("stamp-ledger-event", html);
        Assert.Contains("Actualizado", text);
        Assert.Contains("Sellos actuales 2", text);
        Assert.DoesNotContain("ModernBusiness", text);
        Assert.DoesNotContain("Sellos: 1 -> 2", text);
        Assert.DoesNotContain("secret", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("jwt", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OutboxEmail_UsesOpaqueWalletLinkToken()
    {
        using var fake = WithFakeIntegrations();
        var enrollment = await CreateEnrollmentAsync(fake.Factory, "opaqueweb1");

        using var scope = fake.Factory.Services.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IWalletEmailOutbox>();
        var messages = await outbox.ListAsync();

        Assert.Single(messages);
        Assert.DoesNotContain(enrollment.Card.EnrollmentToken, messages[0].EnrollmentUrl, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(enrollment.Card.EnrollmentToken, ExtractWalletToken(messages[0].EnrollmentUrl));
    }

    [Fact]
    public async Task WalletLanding_LegacyCardIdTokenCanBeDisabled()
    {
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:WalletLinks:AllowLegacyCardIdTokens"] = "false"
        });
        var client = fake.Factory.CreateClient();
        var enrollment = await CreateEnrollmentAsync(fake.Factory, "legacyoff1");
        var publicToken = ExtractWalletToken(enrollment.EnrollmentUrl);

        var legacyHtml = await client.GetStringAsync($"/Wallet/Select/{enrollment.Card.EnrollmentToken}");
        var publicHtml = await client.GetStringAsync($"/Wallet/Select/{publicToken}");

        Assert.Contains("wallet-not-found", legacyHtml);
        Assert.Contains("wallet-select", publicHtml);
    }

    [Fact]
    public async Task Pilot_AllowsAllowedBusinessToEnrollAnyClient()
    {
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:Pilot:Enabled"] = "true",
            ["DigitalCards:Pilot:AllowedBusinessEmails:0"] = "demo@digitalcards.test"
        });
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginBusinessAsync(client);
        var userName = NewLegacySafeUserName("pb");
        await RegisterClientAsync(fake.Factory, userName, $"{userName}@blocked.test");

        var token = await GetAntiforgeryTokenAsync(client, "/Business/Enroll");
        var response = await client.PostAsync(
            "/Business/Enroll",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.UserNameOrEmail"] = userName,
                ["__RequestVerificationToken"] = token
            }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Correo generado", html);
        Assert.DoesNotContain("Este cliente no esta habilitado para el piloto moderno.", html);
    }

    [Fact]
    public async Task Register_RejectsInvalidUserNameAndStoresLowercase()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var getHtml = await client.GetStringAsync("/Register");

        Assert.Contains("pattern=\"[A-Za-z0-9]+\"", getHtml);

        var invalidToken = ExtractAntiforgeryToken(getHtml);
        var invalidResponse = await client.PostAsync(
            "/Register",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.UserName"] = "Bad User",
                ["Input.FirstName"] = "Bad",
                ["Input.LastName"] = "User",
                ["Input.Email"] = "baduser@example.test",
                ["Input.Password"] = "ClientPass123!",
                ["Input.AcceptTerms"] = "true",
                ["__RequestVerificationToken"] = invalidToken
            }));
        var invalidHtml = await invalidResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, invalidResponse.StatusCode);
        Assert.Contains("El usuario solo puede usar letras y numeros, sin espacios.", invalidHtml);

        var validToken = ExtractAntiforgeryToken(invalidHtml);
        var validResponse = await client.PostAsync(
            "/Register",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.UserName"] = "ClientUpper123",
                ["Input.FirstName"] = "Client",
                ["Input.LastName"] = "Upper",
                ["Input.Email"] = "clientupper123@example.test",
                ["Input.Password"] = "ClientPass123!",
                ["Input.AcceptTerms"] = "true",
                ["__RequestVerificationToken"] = validToken
            }));
        var validHtml = await validResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, validResponse.StatusCode);
        Assert.Contains("Cliente clientupper123 registrado.", validHtml);
        Assert.DoesNotContain("ClientUpper123 registrado", validHtml);
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
    public async Task WalletLanding_RemainsPublic_WhenPilotIsEnabled()
    {
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:Pilot:Enabled"] = "true"
        });
        var client = fake.Factory.CreateClient();
        var token = await CreateEnrollmentTokenAsync(fake.Factory, "pilotwallet");

        var html = await client.GetStringAsync($"/Wallet/Select/{token}");

        Assert.Contains("wallet-select", html);
        Assert.Contains("Apple Wallet", html);
        Assert.Contains("Google Wallet", html);
    }

    [Fact]
    public async Task WalletInstallGuidance_RendersPlatformHintsAndTroubleshooting()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient();
        var token = await CreateEnrollmentTokenAsync(fake.Factory, "wguide1");

        var landingHtml = await client.GetStringAsync($"/Wallet/Select/{token}");
        var appleHtml = await client.GetStringAsync($"/Wallet/Apple/{token}");
        var googleHtml = await client.GetStringAsync($"/Wallet/Google/{token}");

        Assert.Contains("wallet-device-guidance", landingHtml);
        Assert.Contains("data-wallet-platform=\"apple\"", landingHtml);
        Assert.Contains("data-wallet-platform=\"google\"", landingHtml);
        Assert.Contains("data-wallet-recommendation", landingHtml);
        Assert.Contains("apple-wallet-install-help", appleHtml);
        Assert.Contains("google-wallet-install-help", googleHtml);
        Assert.DoesNotContain("Authorization", landingHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ApplePass", appleHtml, StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public async Task WalletDiagnostics_WhenEnabled_ReturnsSafeOperationalState()
    {
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:Diagnostics:EnableWalletDiagnostics"] = "true"
        });
        var client = fake.Factory.CreateClient();
        var userName = NewLegacySafeUserName("wd");
        var enrollment = await CreateEnrollmentAsync(fake.Factory, userName);

        await LoginAdminAsync(client);
        var json = await client.GetStringAsync($"/internal/wallet-diagnostics/{enrollment.Card.EnrollmentToken}");

        Assert.Contains("\"clientUserName\"", json);
        Assert.Contains(userName, json);
        Assert.Contains("\"businessName\"", json);
        Assert.Contains("Demo Coffee", json);
        Assert.Contains("\"currentStamps\"", json);
        Assert.DoesNotContain($"{userName}@example.test", json);
        Assert.DoesNotContain("business123", json);
        Assert.DoesNotContain("Password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionString", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WalletDiagnostics_WhenEnabledWithoutAdmin_ReturnsAdminChallenge()
    {
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:Diagnostics:EnableWalletDiagnostics"] = "true"
        });
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var userName = NewLegacySafeUserName("wda");
        var enrollment = await CreateEnrollmentAsync(fake.Factory, userName);

        var response = await client.GetAsync($"/internal/wallet-diagnostics/{enrollment.Card.EnrollmentToken}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Admin/Login", response.Headers.Location?.OriginalString);
    }

    private FakeIntegrationFactory WithFakeIntegrations(
        IReadOnlyDictionary<string, string?>? configurationOverrides = null,
        string? environmentName = null)
    {
        return new FakeIntegrationFactory(_factory, configurationOverrides, environmentName);
    }

    private static IReadOnlyDictionary<string, string?> MySqlUnavailableOverrides()
    {
        return new Dictionary<string, string?>
        {
            ["DigitalCards:PersistenceProvider"] = "MySql",
            ["ConnectionStrings:DigitalCards"] = "Server=127.0.0.1;Port=1;Database=digitalcards;User ID=test;Password=SUPER_SECRET;CharSet=utf8mb4;SslMode=Preferred;Connection Timeout=1;"
        };
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
            ["DigitalCards__Pilot__Enabled"] = "false",
            ["DigitalCards__SkipUserLocalConfiguration"] = "true"
        };

        private readonly Dictionary<string, string?> _previousValues;

        public FakeIntegrationFactory(
            WebApplicationFactory<Program> baseFactory,
            IReadOnlyDictionary<string, string?>? configurationOverrides,
            string? environmentName)
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
                if (!string.IsNullOrWhiteSpace(environmentName))
                {
                    builder.UseEnvironment(environmentName);
                }

                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    var values = new Dictionary<string, string?>
                    {
                        ["DigitalCards:UseFakeIntegrations"] = "true",
                        ["DigitalCards:PersistenceProvider"] = "InMemory",
                        ["DigitalCards:GoogleWallet:Provider"] = "Fake",
                        ["DigitalCards:AppleWallet:Provider"] = "Fake",
                        ["DigitalCards:Email:Provider"] = "Fake",
                        ["DigitalCards:PublicBaseUrl"] = string.Empty,
                        ["DigitalCards:Operations:EnableForwardedHeaders"] = "true",
                        ["DigitalCards:Operations:TrustAllForwardedHeaders"] = "false",
                        ["DigitalCards:Operations:DataProtectionKeysPath"] = string.Empty,
                        ["DigitalCards:Operations:RequireDataProtectionKeysForReadiness"] = "false",
                        ["DigitalCards:Pilot:Enabled"] = "false"
                    };

                    if (configurationOverrides is not null)
                    {
                        foreach (var pair in configurationOverrides)
                        {
                            values[pair.Key] = pair.Value;
                        }
                    }

                    configuration.AddInMemoryCollection(values);
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
        return ExtractWalletToken((await CreateEnrollmentAsync(factory, userName)).EnrollmentUrl);
    }

    private static async Task<EnrollClientResult> CreateEnrollmentAsync(
        WebApplicationFactory<Program> factory,
        string userName,
        string? email = null,
        string password = "")
    {
        using var scope = factory.Services.CreateScope();
        var app = scope.ServiceProvider.GetRequiredService<DigitalCardsAppService>();

        var client = await app.RegisterClientAsync(new RegisterClientCommand(
            userName,
            "Web",
            "Apple",
            email ?? $"{userName}@example.test",
            password));

        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));

        var enrollment = await app.EnrollClientAsync(new EnrollClientCommand(
            business!.Id,
            client.UserName,
            "http://localhost"));

        return enrollment;
    }

    private static Guid SeedOtherBusinessCard(
        WebApplicationFactory<Program> factory,
        string userName)
    {
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<InMemoryDigitalCardsStore>();
        var business = new Business(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Other Business",
            "other@digitalcards.test",
            "business123",
            "/img/demo-coffee.svg");
        var client = new Client(
            Guid.NewGuid(),
            userName,
            "Other",
            "Client",
            $"{userName}@example.test");
        var card = new LoyaltyCard(Guid.NewGuid(), client.Id, business.Id, DateTimeOffset.UtcNow);

        lock (store.Sync)
        {
            if (store.Businesses.All(existing => existing.Id != business.Id))
            {
                store.Businesses.Add(business);
            }

            store.Clients.Add(client);
            store.LoyaltyCards.Add(card);
        }

        return card.Id;
    }

    private static LoyaltyCard FindInMemoryCard(
        WebApplicationFactory<Program> factory,
        Guid cardId)
    {
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<InMemoryDigitalCardsStore>();
        lock (store.Sync)
        {
            return store.LoyaltyCards.Single(card => card.Id == cardId);
        }
    }

    private static LoyaltyCard? FindInMemoryCardOrNull(
        WebApplicationFactory<Program> factory,
        Guid cardId)
    {
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<InMemoryDigitalCardsStore>();
        lock (store.Sync)
        {
            return store.LoyaltyCards.SingleOrDefault(card => card.Id == cardId);
        }
    }

    private static async Task RegisterClientAsync(
        WebApplicationFactory<Program> factory,
        string userName,
        string? email = null,
        string password = "")
    {
        using var scope = factory.Services.CreateScope();
        var app = scope.ServiceProvider.GetRequiredService<DigitalCardsAppService>();

        await app.RegisterClientAsync(new RegisterClientCommand(
            userName,
            "Web",
            "Auth",
            email ?? $"{userName}@example.test",
            password));
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

    private static async Task<HttpResponseMessage> LoginAdminAsync(
        HttpClient client,
        string userNameOrEmail = "admin@digitalcards.test",
        string password = "admin123")
    {
        var token = await GetAntiforgeryTokenAsync(client, "/Admin/Login");
        return await client.PostAsync(
            "/Admin/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.UserNameOrEmail"] = userNameOrEmail,
                ["Input.Password"] = password,
                ["__RequestVerificationToken"] = token
            }));
    }

    private static async Task<HttpResponseMessage> LoginClientAsync(
        HttpClient client,
        string userNameOrEmail,
        string password)
    {
        var token = await GetAntiforgeryTokenAsync(client, "/Client/Login");
        return await client.PostAsync(
            "/Client/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.UserNameOrEmail"] = userNameOrEmail,
                ["Input.Password"] = password,
                ["__RequestVerificationToken"] = token
            }));
    }

    private static async Task<HttpResponseMessage> ChangeClientPasswordAsync(
        HttpClient client,
        string currentPassword,
        string newPassword)
    {
        var token = await GetAntiforgeryTokenAsync(client, "/Client/Profile");
        return await client.PostAsync(
            "/Client/Profile?handler=ChangePassword",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["PasswordInput.CurrentPassword"] = currentPassword,
                ["PasswordInput.NewPassword"] = newPassword,
                ["PasswordInput.ConfirmPassword"] = newPassword,
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

    private static string ExtractWalletToken(string enrollmentUrl)
    {
        const string marker = "/Wallet/Select/";
        var index = enrollmentUrl.IndexOf(marker, StringComparison.Ordinal);
        return index < 0
            ? throw new InvalidOperationException("Wallet link was not found.")
            : enrollmentUrl[(index + marker.Length)..];
    }

    private static string ExtractBusinessEnrollmentToken(string html)
    {
        var match = Regex.Match(html, "/Enroll/(?<token>[A-Za-z0-9_-]+)");
        return match.Success
            ? WebUtility.HtmlDecode(match.Groups["token"].Value)
            : throw new InvalidOperationException("Business enrollment link was not found.");
    }

    private static bool HasBusinessCookie(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("Set-Cookie", out var values) &&
            values.Any(value => value.Contains(".DigitalCards.Business=", StringComparison.Ordinal));
    }

    private static bool HasAdminCookie(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("Set-Cookie", out var values) &&
            values.Any(value => value.Contains(".DigitalCards.Admin=", StringComparison.Ordinal));
    }

    private static bool HasClientCookie(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("Set-Cookie", out var values) &&
            values.Any(value => value.Contains(".DigitalCards.Client=", StringComparison.Ordinal));
    }

    private static bool HasExpiredBusinessCookie(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("Set-Cookie", out var values) &&
            values.Any(value =>
                value.Contains(".DigitalCards.Business=", StringComparison.Ordinal) &&
                value.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasExpiredClientCookie(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("Set-Cookie", out var values) &&
            values.Any(value =>
                value.Contains(".DigitalCards.Client=", StringComparison.Ordinal) &&
                value.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }

    private static string NewLegacySafeUserName(string prefix)
    {
        return $"{prefix}{Guid.NewGuid():N}"[..12];
    }

    private static byte[] TinyPng()
    {
        return Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");
    }

    private static byte[] RectangularPng()
    {
        return Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAIAAAABCAYAAAD0In+KAAAADklEQVR4nGP4z8AAQv8BD/kD/YURmXYAAAAASUVORK5CYII=");
    }

    private static (int Width, int Height) ReadPngDimensions(byte[] bytes)
    {
        Assert.True(bytes.Length >= 24);
        Assert.Equal(TinyPng().AsSpan(0, 8).ToArray(), bytes.AsSpan(0, 8).ToArray());

        var width = ReadBigEndianInt32(bytes.AsSpan(16, 4));
        var height = ReadBigEndianInt32(bytes.AsSpan(20, 4));
        return (width, height);
    }

    private static int ReadBigEndianInt32(ReadOnlySpan<byte> bytes)
    {
        return (bytes[0] << 24) |
            (bytes[1] << 16) |
            (bytes[2] << 8) |
            bytes[3];
    }

    private static string ToLogoPhysicalPath(string uploadRoot, string publicPath)
    {
        const string requestPath = "/uploads/business-logos/";
        Assert.StartsWith(requestPath, publicPath, StringComparison.Ordinal);
        return Path.Combine(
            uploadRoot,
            publicPath[requestPath.Length..].Replace('/', Path.DirectorySeparatorChar));
    }
}
