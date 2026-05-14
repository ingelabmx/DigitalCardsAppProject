using Microsoft.Playwright;

namespace DigitalCards.E2E.Tests;

public sealed class LoyaltyFlowTests : IClassFixture<WebAppFixture>
{
    private readonly WebAppFixture _fixture;

    public LoyaltyFlowTests(WebAppFixture fixture)
    {
        _fixture = fixture;
    }

    [PlaywrightFact]
    public async Task AdminCanCreateAndResetAdminAccess_WithFakeServices()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        var suffix = NewLegacySafeUserName("aa");
        var userName = $"a{suffix[..8]}";
        var email = $"{suffix}@ad.test";
        const string initialPassword = "NewAdmin123!";
        const string resetPassword = "ChangedAdmin123!";

        await page.GotoAsync(new Uri(_fixture.BaseAddress, "/Admin/Login").ToString());
        await page.GetByTestId("admin-username").FillAsync(GetAdminEmail());
        await page.GetByTestId("admin-password").FillAsync(GetAdminPassword());
        await page.GetByTestId("admin-login-submit").ClickAsync();
        await page.GetByTestId("admin-users-link").ClickAsync();
        await page.GetByTestId("admin-users-create-link").ClickAsync();
        await page.GetByTestId("admin-create-admin-username").FillAsync(userName);
        await page.GetByTestId("admin-create-admin-first-name").FillAsync("Playwright");
        await page.GetByTestId("admin-create-admin-last-name").FillAsync("Admin");
        await page.GetByTestId("admin-create-admin-email").FillAsync(email);
        await page.GetByTestId("admin-create-admin-password").FillAsync(initialPassword);
        await page.GetByTestId("admin-create-admin-confirm-password").FillAsync(initialPassword);
        await page.GetByTestId("admin-create-admin-submit").ClickAsync();

        Assert.Contains("Admin creado", await page.GetByTestId("admin-create-admin-status").InnerTextAsync());
        Assert.DoesNotContain(initialPassword, await page.ContentAsync(), StringComparison.Ordinal);

        await page.GetByTestId("admin-created-admin-open-link").ClickAsync();
        var createdAdminRow = page.GetByTestId("admin-user-row").Filter(new LocatorFilterOptions { HasText = userName });
        await createdAdminRow.GetByTestId("admin-user-reset-password").FillAsync(resetPassword);
        await createdAdminRow.GetByTestId("admin-user-reset-confirm").FillAsync(resetPassword);
        await createdAdminRow.GetByTestId("admin-user-reset-submit").ClickAsync();
        Assert.Contains("Contrasena de admin actualizada", await page.GetByTestId("admin-users-status").InnerTextAsync());
        Assert.DoesNotContain(resetPassword, await page.ContentAsync(), StringComparison.Ordinal);

        await page.GotoAsync(new Uri(_fixture.BaseAddress, "/Admin/Logout").ToString());
        await page.GetByTestId("admin-username").FillAsync(userName);
        await page.GetByTestId("admin-password").FillAsync(initialPassword);
        await page.GetByTestId("admin-login-submit").ClickAsync();
        Assert.Contains("Credenciales de admin invalidas", await page.ContentAsync());

        await page.GotoAsync(new Uri(_fixture.BaseAddress, "/Admin/Login").ToString());
        await page.GetByTestId("admin-username").FillAsync(userName);
        await page.GetByTestId("admin-password").FillAsync(resetPassword);
        await page.GetByTestId("admin-login-submit").ClickAsync();
        Assert.Contains("Playwright Admin", await page.GetByTestId("admin-dashboard-title").InnerTextAsync());
        await page.GetByTestId("admin-businesses-link").ClickAsync();
        Assert.True(await page.GetByTestId("admin-business-search-form").IsVisibleAsync());
    }

    [PlaywrightFact]
    public async Task AdminCreatesBusinessAndBusinessCanUseModernFlow_WithFakeServices()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        var suffix = NewLegacySafeUserName("bz");
        var businessName = $"Biz {suffix[..8]}";
        var businessEmail = $"{suffix}@biz.test";
        var updatedBusinessName = $"Upd {suffix[..8]}";
        var updatedBusinessEmail = $"{suffix}@up.test";
        var publicBrandName = $"Brand {suffix[..8]}";
        const string businessPassword = "StartPass123!";
        const string updatedBusinessPassword = "ChangedPass123!";
        var userName = NewLegacySafeUserName("u");
        const string clientPassword = "ClientPass123!";

        await page.GotoAsync(new Uri(_fixture.BaseAddress, "/Admin/Login").ToString());
        await page.GetByTestId("admin-username").FillAsync(GetAdminEmail());
        await page.GetByTestId("admin-password").FillAsync(GetAdminPassword());
        await page.GetByTestId("admin-login-submit").ClickAsync();
        await page.GetByTestId("admin-create-business-link").ClickAsync();
        await page.GetByTestId("admin-create-business-name").FillAsync(businessName);
        await page.GetByTestId("admin-create-business-email").FillAsync(businessEmail);
        await page.GetByTestId("admin-create-business-password").FillAsync(businessPassword);
        await page.GetByTestId("admin-create-business-confirm-password").FillAsync(businessPassword);
        await page.GetByTestId("admin-create-business-enable-pilot").CheckAsync();
        await page.GetByTestId("admin-create-business-notes").FillAsync("creado desde playwright");
        await page.GetByTestId("admin-create-business-submit").ClickAsync();

        Assert.Contains("Negocio creado", await page.GetByTestId("admin-create-business-status").InnerTextAsync());
        Assert.Contains("Piloto habilitado", await page.GetByTestId("admin-created-business-result").InnerTextAsync());
        Assert.DoesNotContain(businessPassword, await page.ContentAsync(), StringComparison.Ordinal);

        await page.GetByTestId("admin-created-business-open-link").ClickAsync();
        await page.GetByTestId("admin-manage-business").First.ClickAsync();
        await page.GetByTestId("admin-business-profile-name").FillAsync(updatedBusinessName);
        await page.GetByTestId("admin-business-profile-email").FillAsync(updatedBusinessEmail);
        await page.GetByTestId("admin-business-profile-logo").FillAsync("~/Logos/playwright.png");
        await page.GetByTestId("admin-business-profile-notes").FillAsync("actualizado desde playwright");
        await page.GetByTestId("admin-business-profile-save").ClickAsync();
        Assert.Contains("Negocio actualizado", await page.GetByTestId("admin-business-profile-status").InnerTextAsync());

        var logoFile = Path.Combine(Path.GetTempPath(), $"playwright-logo-{Guid.NewGuid():N}.png");
        await File.WriteAllBytesAsync(logoFile, TinyPng());
        try
        {
            await page.GetByTestId("admin-business-branding-public-name").FillAsync(publicBrandName);
            await page.GetByTestId("admin-business-branding-program-name").FillAsync("Playwright Rewards");
            await page.GetByTestId("admin-business-branding-description").FillAsync("Programa de sellos para Playwright.");
            await page.GetByTestId("admin-business-branding-logo").FillAsync("/img/playwright-brand.svg");
            await page.GetByTestId("admin-business-branding-logo-upload").SetInputFilesAsync(logoFile);
            await page.GetByTestId("admin-business-branding-primary").FillAsync("#123456");
            await page.GetByTestId("admin-business-branding-secondary").FillAsync("#abcdef");
            await page.GetByTestId("admin-business-branding-submit").ClickAsync();
            Assert.Contains("Branding del negocio actualizado", await page.GetByTestId("admin-business-profile-status").InnerTextAsync());
            await page.GotoAsync(page.Url.Split('?')[0]);
            Assert.Contains(
                "/uploads/business-logos/",
                await page.GetByTestId("admin-business-branding-logo").InputValueAsync());
        }
        finally
        {
            if (File.Exists(logoFile))
            {
                File.Delete(logoFile);
            }
        }

        await page.GetByTestId("admin-business-password-new").FillAsync(updatedBusinessPassword);
        await page.GetByTestId("admin-business-password-confirm").FillAsync(updatedBusinessPassword);
        await page.GetByTestId("admin-business-password-submit").ClickAsync();
        Assert.Contains("Contrasena de negocio actualizada", await page.GetByTestId("admin-business-profile-status").InnerTextAsync());
        Assert.DoesNotContain(updatedBusinessPassword, await page.ContentAsync(), StringComparison.Ordinal);

        await page.GotoAsync(new Uri(_fixture.BaseAddress, "/Register").ToString());
        await page.GetByTestId("register-username").FillAsync(userName);
        await page.GetByTestId("register-first-name").FillAsync("Nuevo");
        await page.GetByTestId("register-last-name").FillAsync("Cliente");
        await page.GetByTestId("register-email").FillAsync($"{userName}@e.test");
        await page.GetByTestId("register-password").FillAsync(clientPassword);
        await page.GetByTestId("register-submit").ClickAsync();

        await page.GotoAsync(new Uri(_fixture.BaseAddress, "/Business/Login").ToString());
        await page.GetByTestId("business-email").FillAsync(updatedBusinessEmail);
        await page.GetByTestId("business-password").FillAsync(updatedBusinessPassword);
        await page.GetByTestId("business-login-submit").ClickAsync();
        Assert.Contains(updatedBusinessName, await page.GetByTestId("business-dashboard-title").InnerTextAsync());

        await page.GetByTestId("enroll-link").ClickAsync();
        await page.GetByTestId("enroll-username").FillAsync(userName);
        await page.GetByTestId("enroll-submit").ClickAsync();
        Assert.Contains("Correo generado", await page.GetByTestId("enroll-success").InnerTextAsync());

        await page.GotoAsync(new Uri(_fixture.BaseAddress, "/Dev/Outbox").ToString());
        await page.GetByTestId("email-link").First.ClickAsync();
        Assert.Contains(publicBrandName, await page.GetByTestId("wallet-select").InnerTextAsync());
    }

    [PlaywrightFact]
    public async Task ClientBusinessGoogleWalletAndStampFlow_WorksWithFakeServices()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        var userName = NewLegacySafeUserName("m");
        var businessEmail = GetBusinessEmail();
        var businessPassword = GetBusinessPassword();
        var businessName = GetBusinessName();
        const string clientPassword = "ClientPass123!";

        await EnableDemoBusinessPilotAsync(page);

        await page.GotoAsync(new Uri(_fixture.BaseAddress, "/Register").ToString());
        await page.GetByTestId("register-username").FillAsync(userName);
        await page.GetByTestId("register-first-name").FillAsync("Maria");
        await page.GetByTestId("register-last-name").FillAsync("Lopez");
        await page.GetByTestId("register-email").FillAsync($"{userName}@e.test");
        await page.GetByTestId("register-password").FillAsync(clientPassword);
        await page.GetByTestId("register-submit").ClickAsync();
        Assert.Contains("registrado", await page.GetByTestId("register-success").InnerTextAsync());

        await page.GotoAsync(new Uri(_fixture.BaseAddress, "/Business/Login").ToString());
        await page.GetByTestId("business-email").FillAsync(businessEmail);
        await page.GetByTestId("business-password").FillAsync(businessPassword);
        await page.GetByTestId("business-login-submit").ClickAsync();
        Assert.Contains(businessName, await page.GetByTestId("business-dashboard-title").InnerTextAsync());

        var enrollUrl = await page.GetByTestId("enroll-link").GetAttributeAsync("href");
        var cardsUrl = await page.GetByTestId("cards-link").GetAttributeAsync("href");
        Assert.DoesNotContain("businessId", enrollUrl, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("businessId", cardsUrl, StringComparison.OrdinalIgnoreCase);
        await page.GetByTestId("enroll-link").ClickAsync();
        await page.GetByTestId("enroll-username").FillAsync(userName);
        await page.GetByTestId("enroll-submit").ClickAsync();
        Assert.Contains("Correo generado", await page.GetByTestId("enroll-success").InnerTextAsync());

        await page.GotoAsync(new Uri(_fixture.BaseAddress, "/Dev/Outbox").ToString());
        await page.GetByTestId("email-link").First.ClickAsync();
        Assert.Contains(businessName, await page.GetByTestId("wallet-select").InnerTextAsync());
        var walletLandingUrl = page.Url;

        await page.GetByTestId("apple-wallet-button").ClickAsync();
        Assert.Contains("Apple Wallet pendiente", await page.GetByTestId("apple-wallet-pending").InnerTextAsync());

        await page.GotoAsync(walletLandingUrl);
        await page.GetByTestId("google-wallet-button").ClickAsync();
        Assert.Contains("fake-google-", await page.GetByTestId("google-object-id").InnerTextAsync());

        await page.GotoAsync(new Uri(_fixture.BaseAddress, cardsUrl!).ToString());
        await page.GetByTestId("business-card-search-input").FillAsync(userName);
        await page.GetByTestId("business-card-search-submit").ClickAsync();
        await page.GetByTestId("business-card-result").First.ClickAsync();
        Assert.Contains(userName, await page.GetByTestId("business-card-detail").InnerTextAsync());
        await page.GetByTestId("business-card-resend-submit").ClickAsync();
        Assert.Contains("Correo reenviado", await page.GetByTestId("business-card-status").InnerTextAsync());
        await page.GetByTestId("business-card-stamp-submit").ClickAsync();
        Assert.Equal("2", await page.GetByTestId("business-card-current-stamps").InnerTextAsync());
        Assert.Equal("Emitida", await page.GetByTestId("business-card-google-status").InnerTextAsync());
        var ledgerText = await page.GetByTestId("stamp-ledger-list").InnerTextAsync();
        Assert.Contains("ModernBusiness", ledgerText);
        Assert.Contains("Sellos: 1 -> 2", ledgerText);

        await page.GotoAsync(new Uri(_fixture.BaseAddress, "/Business/Dashboard").ToString());
        Assert.True(await page.GetByTestId("business-dashboard-summary").IsVisibleAsync());
        Assert.Contains(userName, await page.GetByTestId("business-dashboard-recent-cards").InnerTextAsync());
        Assert.Contains("ModernBusiness", await page.GetByTestId("business-dashboard-ledger").InnerTextAsync());

        await page.GotoAsync(new Uri(_fixture.BaseAddress, "/Client/Login").ToString());
        await page.GetByTestId("client-login-username").FillAsync(userName);
        await page.GetByTestId("client-login-password").FillAsync(clientPassword);
        await page.GetByTestId("client-login-submit").ClickAsync();
        Assert.Contains("Maria Lopez", await page.GetByTestId("client-dashboard-title").InnerTextAsync());
        Assert.Contains("1 tarjeta", await page.GetByTestId("client-dashboard-card-count").InnerTextAsync());
        Assert.Equal("2", await page.GetByTestId("client-dashboard-current-stamps").InnerTextAsync());
        Assert.True(await page.GetByTestId("client-qr-card").Locator("svg").IsVisibleAsync());
        Assert.Contains(userName, await page.GetByTestId("client-profile-summary").InnerTextAsync());
        await page.GetByTestId("client-dashboard-cards-link").ClickAsync();
        var cardText = await page.GetByTestId("client-card-results").InnerTextAsync();
        Assert.Equal("2", await page.GetByTestId("client-card-current-stamps").First.InnerTextAsync());
        Assert.Contains("Google emitida", cardText);
        Assert.Contains("Apple Wallet", cardText);
        Assert.True(await page.GetByTestId("client-cards-qr-card").Locator("svg").IsVisibleAsync());

        const string changedClientPassword = "ChangedClient123!";
        await page.GotoAsync(new Uri(_fixture.BaseAddress, "/Client/Dashboard").ToString());
        await page.GetByTestId("client-dashboard-change-password-link").ClickAsync();
        await page.GetByTestId("client-current-password").FillAsync(clientPassword);
        await page.GetByTestId("client-new-password").FillAsync(changedClientPassword);
        await page.GetByTestId("client-confirm-password").FillAsync(changedClientPassword);
        await page.GetByTestId("client-change-password-submit").ClickAsync();
        Assert.Contains("Contrasena de cliente actualizada", await page.GetByTestId("client-change-password-status").InnerTextAsync());
        Assert.DoesNotContain(changedClientPassword, await page.ContentAsync(), StringComparison.Ordinal);

        await page.GotoAsync(new Uri(_fixture.BaseAddress, "/Client/Logout").ToString());
        await page.GetByTestId("client-login-username").FillAsync(userName);
        await page.GetByTestId("client-login-password").FillAsync(clientPassword);
        await page.GetByTestId("client-login-submit").ClickAsync();
        Assert.Contains("Credenciales de cliente invalidas", await page.ContentAsync());

        await page.GotoAsync(new Uri(_fixture.BaseAddress, "/Client/Login").ToString());
        await page.GetByTestId("client-login-username").FillAsync(userName);
        await page.GetByTestId("client-login-password").FillAsync(changedClientPassword);
        await page.GetByTestId("client-login-submit").ClickAsync();
        Assert.Contains("Maria Lopez", await page.GetByTestId("client-dashboard-title").InnerTextAsync());

        await DisableDemoBusinessPilotAsync(page);
        await page.GotoAsync(new Uri(_fixture.BaseAddress, "/Business/Dashboard").ToString());
        Assert.Contains("no esta habilitado", await page.GetByTestId("pilot-business-blocked").InnerTextAsync());
    }

    [PlaywrightFact]
    public async Task WalletLanding_IsResponsiveForIPhoneAndAndroidViewports()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        var userName = NewLegacySafeUserName("p");

        await EnableDemoBusinessPilotAsync(page);
        await CreateEnrollmentAsync(page, userName);
        await page.GotoAsync(new Uri(_fixture.BaseAddress, "/Dev/Outbox").ToString());
        var enrollmentUrl = await page.GetByTestId("email-link").First.GetAttributeAsync("href");

        foreach (var viewport in new[] { new ViewportSize { Width = 390, Height = 844 }, new ViewportSize { Width = 412, Height = 915 } })
        {
            await page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            await page.GotoAsync(enrollmentUrl!);

            Assert.True(await page.GetByTestId("apple-wallet-button").IsVisibleAsync());
            Assert.True(await page.GetByTestId("google-wallet-button").IsVisibleAsync());

            var hasHorizontalOverflow = await page.EvaluateAsync<bool>(
                "() => document.documentElement.scrollWidth > window.innerWidth + 1");
            Assert.False(hasHorizontalOverflow);
        }
    }

    private async Task CreateEnrollmentAsync(IPage page, string userName)
    {
        await page.GotoAsync(new Uri(_fixture.BaseAddress, "/Register").ToString());
        await page.GetByTestId("register-username").FillAsync(userName);
        await page.GetByTestId("register-first-name").FillAsync("Phone");
        await page.GetByTestId("register-last-name").FillAsync("User");
        await page.GetByTestId("register-email").FillAsync($"{userName}@e.test");
        await page.GetByTestId("register-password").FillAsync("ClientPass123!");
        await page.GetByTestId("register-submit").ClickAsync();

        await page.GotoAsync(new Uri(_fixture.BaseAddress, "/Business/Login").ToString());
        await page.GetByTestId("business-email").FillAsync(GetBusinessEmail());
        await page.GetByTestId("business-password").FillAsync(GetBusinessPassword());
        await page.GetByTestId("business-login-submit").ClickAsync();
        await page.GetByTestId("enroll-link").ClickAsync();
        await page.GetByTestId("enroll-username").FillAsync(userName);
        await page.GetByTestId("enroll-submit").ClickAsync();
    }

    private async Task EnableDemoBusinessPilotAsync(IPage page)
    {
        await page.GotoAsync(new Uri(_fixture.BaseAddress, "/Admin/Login").ToString());
        await page.GetByTestId("admin-username").FillAsync(GetAdminEmail());
        await page.GetByTestId("admin-password").FillAsync(GetAdminPassword());
        await page.GetByTestId("admin-login-submit").ClickAsync();
        Assert.Contains("Admin", await page.GetByTestId("admin-dashboard-title").InnerTextAsync());

        await page.GetByTestId("admin-businesses-link").ClickAsync();
        await page.GetByTestId("admin-business-search-input").FillAsync(GetBusinessEmail());
        await page.GetByTestId("admin-business-search-submit").ClickAsync();
        await page.GetByTestId("admin-enable-pilot").First.ClickAsync();
        Assert.Contains("Piloto habilitado", await page.GetByTestId("admin-business-status").InnerTextAsync());
    }

    private async Task DisableDemoBusinessPilotAsync(IPage page)
    {
        await page.GotoAsync(new Uri(_fixture.BaseAddress, "/Admin/Businesses").ToString());
        await page.GetByTestId("admin-business-search-input").FillAsync(GetBusinessEmail());
        await page.GetByTestId("admin-business-search-submit").ClickAsync();
        await page.GetByTestId("admin-disable-pilot").First.ClickAsync();
        Assert.Contains("Piloto deshabilitado", await page.GetByTestId("admin-business-status").InnerTextAsync());
    }

    private static string NewLegacySafeUserName(string prefix)
    {
        return $"{prefix}{Guid.NewGuid():N}"[..12];
    }

    private static string GetBusinessEmail()
    {
        return Environment.GetEnvironmentVariable("E2E_BUSINESS_EMAIL") ?? "demo@digitalcards.test";
    }

    private static string GetBusinessPassword()
    {
        return Environment.GetEnvironmentVariable("E2E_BUSINESS_PASSWORD") ?? "business123";
    }

    private static string GetBusinessName()
    {
        return Environment.GetEnvironmentVariable("E2E_BUSINESS_NAME") ?? "Demo Coffee";
    }

    private static string GetAdminEmail()
    {
        return Environment.GetEnvironmentVariable("E2E_ADMIN_EMAIL") ?? "admin@digitalcards.test";
    }

    private static string GetAdminPassword()
    {
        return Environment.GetEnvironmentVariable("E2E_ADMIN_PASSWORD") ?? "admin123";
    }

    private static byte[] TinyPng()
    {
        return Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");
    }
}
