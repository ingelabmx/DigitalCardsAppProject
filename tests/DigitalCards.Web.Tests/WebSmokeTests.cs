using System.Net;
using System.Text.RegularExpressions;
using DigitalCards.Application;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Domain;
using DigitalCards.Infrastructure.Persistence;
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
    public async Task AdminPages_WithoutCookie_RedirectToLogin()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        foreach (var path in new[] { "/Admin/Dashboard", "/Admin/Businesses", "/Admin/CreateBusiness", "/Admin/BusinessProfile/11111111-1111-1111-1111-111111111111", "/Admin/AdminUsers", "/Admin/CreateAdmin", "/Admin/Clients" })
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
        Assert.Contains("Piloto habilitado", enableHtml);

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
        Assert.Contains("Piloto deshabilitado", disableHtml);

        var blockedAgainHtml = await client.GetStringAsync("/Business/Dashboard");
        Assert.Contains("pilot-business-blocked", blockedAgainHtml);
    }

    [Fact]
    public async Task AdminClients_EnableAndDisablePilotClientRecordsState()
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
        var userName = NewLegacySafeUserName("pc");
        var email = $"{userName}@blocked.test";
        await RegisterClientAsync(fake.Factory, userName, email);
        Guid clientId;
        using (var scope = fake.Factory.Services.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<InMemoryDigitalCardsStore>();
            clientId = store.Clients.Single(existing => existing.UserName == userName).Id;
        }

        await LoginAdminAsync(client);
        var searchHtml = await client.GetStringAsync($"/Admin/Clients?Query={userName}");
        Assert.Contains("admin-client-row", searchHtml);
        Assert.Contains(userName, searchHtml);
        Assert.Contains(email, searchHtml);
        Assert.DoesNotContain("password", searchHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash", searchHtml, StringComparison.OrdinalIgnoreCase);

        var enableToken = ExtractAntiforgeryToken(searchHtml);
        var enableResponse = await client.PostAsync(
            $"/Admin/Clients?handler=Enable&Query={userName}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["clientId"] = clientId.ToString(),
                ["notes"] = "cliente piloto web test",
                ["__RequestVerificationToken"] = enableToken
            }));
        var enableHtml = await enableResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
        Assert.Contains("Piloto habilitado", enableHtml);

        var disableToken = ExtractAntiforgeryToken(enableHtml);
        var disableResponse = await client.PostAsync(
            $"/Admin/Clients?handler=Disable&Query={userName}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["clientId"] = clientId.ToString(),
                ["__RequestVerificationToken"] = disableToken
            }));
        var disableHtml = await disableResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, disableResponse.StatusCode);
        Assert.Contains("Piloto deshabilitado", disableHtml);
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
        Assert.Contains("Piloto habilitado", html);
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
        Assert.Contains("Piloto deshabilitado", html);
        Assert.DoesNotContain(password, html, StringComparison.Ordinal);

        var loginResponse = await LoginBusinessAsync(client, businessEmail, password);
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        var dashboardHtml = await client.GetStringAsync("/Business/Dashboard");

        Assert.Contains("pilot-business-blocked", dashboardHtml);
        Assert.Contains("Este negocio no esta habilitado para el piloto moderno.", dashboardHtml);
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
                ["Input.Notes"] = "actualizado por web test",
                ["__RequestVerificationToken"] = saveToken
            }));
        var saveHtml = await saveResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
        Assert.Contains("Negocio actualizado", saveHtml);
        Assert.Contains(updatedName, saveHtml);
        Assert.Contains("~/Logos/updated.png", saveHtml);

        var resetToken = ExtractAntiforgeryToken(saveHtml);
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

        foreach (var path in new[] { "/Business/Dashboard", "/Business/Enroll", "/Business/Stamp", "/Business/Cards" })
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
    public async Task ClientPages_WithoutCookie_RedirectToLogin()
    {
        using var fake = WithFakeIntegrations();
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        foreach (var path in new[] { "/Client/Dashboard", "/Client/Cards" })
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

        Assert.Contains("client-card-results", html);
        Assert.Contains(firstUserName, html);
        Assert.Contains("Demo Coffee", html);
        Assert.DoesNotContain(secondUserName, html);
        Assert.DoesNotContain("client-cards-username", html);
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
            ["DigitalCards:Pilot:AllowedBusinessEmails:0"] = "demo@digitalcards.test",
            ["DigitalCards:Pilot:AllowedClientEmailDomains:0"] = "example.test"
        });
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginBusinessAsync(client);

        foreach (var path in new[] { "/Business/Dashboard", "/Business/Enroll", "/Business/Stamp", "/Business/Cards" })
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
            ["DigitalCards:Pilot:AllowedBusinessIds:0"] = "11111111-1111-1111-1111-111111111111",
            ["DigitalCards:Pilot:AllowedClientEmailDomains:0"] = "example.test"
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
    public async Task Pilot_WithBlockedBusiness_ShowsMessageAndHidesModernActions()
    {
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:Pilot:Enabled"] = "true",
            ["DigitalCards:Pilot:AllowedBusinessEmails:0"] = "other@digitalcards.test",
            ["DigitalCards:Pilot:AllowedClientEmailDomains:0"] = "example.test"
        });
        var client = fake.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginBusinessAsync(client);

        var html = await client.GetStringAsync("/Business/Dashboard");

        Assert.Contains("pilot-business-blocked", html);
        Assert.Contains("Este negocio no esta habilitado para el piloto moderno.", html);
        Assert.DoesNotContain("data-testid=\"enroll-link\"", html);
        Assert.DoesNotContain("data-testid=\"cards-link\"", html);

        foreach (var path in new[] { "/Business/Enroll", "/Business/Stamp", "/Business/Cards" })
        {
            var blockedPage = await client.GetStringAsync(path);

            Assert.Contains("pilot-business-blocked", blockedPage);
            Assert.DoesNotContain("data-testid=\"enroll-form\"", blockedPage);
            Assert.DoesNotContain("data-testid=\"stamp-form\"", blockedPage);
            Assert.DoesNotContain("data-testid=\"business-card-search-form\"", blockedPage);
        }
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

        var html = await client.GetStringAsync($"/Business/Cards?Query={userName}");

        Assert.Contains("business-card-results", html);
        Assert.Contains(userName, html);
        Assert.Contains(enrollment.Card.Id.ToString(), html);
        Assert.DoesNotContain("othercard1", html);
        Assert.DoesNotContain("businessId", html, StringComparison.OrdinalIgnoreCase);
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
        Assert.Contains("ModernBusiness", text);
        Assert.Contains("Sellos: 1 -> 2", text);
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
    public async Task Pilot_AllowsAllowedBusinessToEnrollClientOutsideClientAllowlist()
    {
        using var fake = WithFakeIntegrations(new Dictionary<string, string?>
        {
            ["DigitalCards:Pilot:Enabled"] = "true",
            ["DigitalCards:Pilot:AllowedBusinessEmails:0"] = "demo@digitalcards.test",
            ["DigitalCards:Pilot:AllowedClientEmailDomains:0"] = "allowed.test"
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

    private FakeIntegrationFactory WithFakeIntegrations(IReadOnlyDictionary<string, string?>? configurationOverrides = null)
    {
        return new FakeIntegrationFactory(_factory, configurationOverrides);
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
            IReadOnlyDictionary<string, string?>? configurationOverrides)
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
}
