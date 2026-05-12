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
    public async Task ClientBusinessGoogleWalletAndStampFlow_WorksWithFakeServices()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        var userName = NewLegacySafeUserName("m");
        var businessEmail = GetBusinessEmail();
        var businessPassword = GetBusinessPassword();
        var businessName = GetBusinessName();

        await page.GotoAsync(new Uri(_fixture.BaseAddress, "/Register").ToString());
        await page.GetByTestId("register-username").FillAsync(userName);
        await page.GetByTestId("register-first-name").FillAsync("Maria");
        await page.GetByTestId("register-last-name").FillAsync("Lopez");
        await page.GetByTestId("register-email").FillAsync($"{userName}@e.test");
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

        await page.GotoAsync(new Uri(_fixture.BaseAddress, $"/Client/Cards?UserName={userName}").ToString());
        var cardText = await page.GetByTestId("client-card-results").InnerTextAsync();
        Assert.Contains("Sellos actuales: 2", cardText);
        Assert.Contains("Google emitida", cardText);
    }

    [PlaywrightFact]
    public async Task WalletLanding_IsResponsiveForIPhoneAndAndroidViewports()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        var userName = NewLegacySafeUserName("p");

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
        await page.GetByTestId("register-submit").ClickAsync();

        await page.GotoAsync(new Uri(_fixture.BaseAddress, "/Business/Login").ToString());
        await page.GetByTestId("business-email").FillAsync(GetBusinessEmail());
        await page.GetByTestId("business-password").FillAsync(GetBusinessPassword());
        await page.GetByTestId("business-login-submit").ClickAsync();
        await page.GetByTestId("enroll-link").ClickAsync();
        await page.GetByTestId("enroll-username").FillAsync(userName);
        await page.GetByTestId("enroll-submit").ClickAsync();
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
}
