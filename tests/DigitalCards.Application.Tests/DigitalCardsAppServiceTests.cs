using DigitalCards.Application;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Domain;
using DigitalCards.Infrastructure;
using DigitalCards.Infrastructure.Persistence;
using DigitalCards.Infrastructure.Email;
using DigitalCards.Infrastructure.Persistence.MySql;
using DigitalCards.Infrastructure.Wallets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalCards.Application.Tests;

public sealed class DigitalCardsAppServiceTests
{
    [Fact]
    public void AddInfrastructure_ThrowsWhenMySqlProviderHasNoConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DigitalCards:PersistenceProvider"] = "MySql"
            })
            .Build();

        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddDigitalCardsInfrastructure(configuration));

        Assert.Contains("ConnectionStrings:DigitalCards", exception.Message);
    }

    [Fact]
    public void AddInfrastructure_ThrowsWhenPersistenceProviderIsUnknown()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DigitalCards:PersistenceProvider"] = "SqlServer"
            })
            .Build();

        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddDigitalCardsInfrastructure(configuration));

        Assert.Contains("DigitalCards:PersistenceProvider", exception.Message);
        Assert.DoesNotContain("Password", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddInfrastructure_ThrowsWhenMySqlConnectionStringIsIncomplete()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DigitalCards:PersistenceProvider"] = "MySql",
                ["ConnectionStrings:DigitalCards"] = "Server=localhost;Database=dcards_test;"
            })
            .Build();

        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddDigitalCardsInfrastructure(configuration));

        Assert.Contains("ConnectionStrings:DigitalCards User ID", exception.Message);
    }

    [Fact]
    public void AddInfrastructure_RegistersMySqlRepositoriesWhenConfigured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DigitalCards:PersistenceProvider"] = "MySql",
                ["ConnectionStrings:DigitalCards"] = "Server=localhost;Database=dcards_test;User ID=test;Password=test;"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDigitalCardsInfrastructure(configuration);

        var provider = services.BuildServiceProvider();

        Assert.IsType<MySqlClientRepository>(provider.GetRequiredService<IClientRepository>());
        Assert.IsType<MySqlBusinessRepository>(provider.GetRequiredService<IBusinessRepository>());
        Assert.IsType<MySqlAdminUserRepository>(provider.GetRequiredService<IAdminUserRepository>());
        Assert.IsType<MySqlBusinessCredentialRepository>(provider.GetRequiredService<IBusinessCredentialRepository>());
        Assert.IsType<MySqlLoyaltyCardRepository>(provider.GetRequiredService<ILoyaltyCardRepository>());
        Assert.IsType<MySqlAppleWalletPassRepository>(provider.GetRequiredService<IAppleWalletPassRepository>());
        Assert.IsType<MySqlWalletLinkTokenRepository>(provider.GetRequiredService<IWalletLinkTokenRepository>());
        Assert.IsType<MySqlStampLedgerRepository>(provider.GetRequiredService<IStampLedgerRepository>());
        Assert.IsType<MySqlPilotBusinessRepository>(provider.GetRequiredService<IPilotBusinessRepository>());
    }

    [Fact]
    public void AddInfrastructure_RegistersFakeGoogleWalletByDefault()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDigitalCardsInfrastructure(new ConfigurationBuilder().Build());

        var provider = services.BuildServiceProvider();

        Assert.IsType<FakeGoogleWalletService>(provider.GetRequiredService<IGoogleWalletService>());
        Assert.IsType<FakeAppleWalletService>(provider.GetRequiredService<IAppleWalletService>());
        Assert.IsType<FakeAppleWalletPushSender>(provider.GetRequiredService<IAppleWalletPushSender>());
        Assert.IsType<FakeWalletEmailOutbox>(provider.GetRequiredService<IEmailSender>());
        Assert.IsType<InMemoryAdminUserRepository>(provider.GetRequiredService<IAdminUserRepository>());
        Assert.IsType<InMemoryBusinessCredentialRepository>(provider.GetRequiredService<IBusinessCredentialRepository>());
        Assert.IsType<InMemoryWalletLinkTokenRepository>(provider.GetRequiredService<IWalletLinkTokenRepository>());
        Assert.IsType<InMemoryStampLedgerRepository>(provider.GetRequiredService<IStampLedgerRepository>());
        Assert.IsType<InMemoryPilotBusinessRepository>(provider.GetRequiredService<IPilotBusinessRepository>());
    }

    [Fact]
    public async Task LoginAdmin_WithLegacyRoleAdminCredentials_ReturnsAdmin()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var adminApp = provider.GetRequiredService<AdminAppService>();

        var admin = await adminApp.LoginAdminAsync(new AdminLoginCommand(
            "admin@digitalcards.test",
            "admin123"));

        Assert.NotNull(admin);
        Assert.Equal("admin@digitalcards.test", admin!.Email);
        Assert.Equal("DCAdmin", admin.UserName);
    }

    [Fact]
    public async Task LoginAdmin_WithBusinessCredentials_ReturnsNull()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var adminApp = provider.GetRequiredService<AdminAppService>();

        var admin = await adminApp.LoginAdminAsync(new AdminLoginCommand(
            "demo@digitalcards.test",
            "business123"));

        Assert.Null(admin);
    }

    [Fact]
    public async Task SetPilotBusinessAsync_UpsertsPilotState()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var adminApp = provider.GetRequiredService<AdminAppService>();
        var business = await provider.GetRequiredService<IBusinessRepository>()
            .FindByEmailAsync("demo@digitalcards.test");
        var admin = await adminApp.LoginAdminAsync(new AdminLoginCommand(
            "DCAdmin",
            "admin123"));

        var enabled = await adminApp.SetPilotBusinessAsync(new SetPilotBusinessCommand(
            business!.Id,
            admin!.Id,
            IsEnabled: true,
            Notes: "piloto inicial"));
        var disabled = await adminApp.SetPilotBusinessAsync(new SetPilotBusinessCommand(
            business.Id,
            admin.Id,
            IsEnabled: false,
            Notes: "pausado"));
        var businesses = await adminApp.ListPilotBusinessesAsync("demo");

        Assert.NotNull(enabled);
        Assert.True(enabled!.IsEnabled);
        Assert.NotNull(disabled);
        Assert.False(disabled!.IsEnabled);
        var listed = Assert.Single(businesses);
        Assert.False(listed.IsEnabled);
        Assert.Equal("pausado", listed.Notes);
    }

    [Fact]
    public async Task LoginBusiness_WithLegacyPassword_CreatesModernCredential()
    {
        var services = CreateDefaultServices();
        var provider = services.BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var credentials = provider.GetRequiredService<IBusinessCredentialRepository>();

        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));

        Assert.NotNull(business);
        var credential = await credentials.FindByBusinessIdAsync(business!.Id);
        Assert.NotNull(credential);
        Assert.DoesNotContain("business123", credential!.PasswordHash, StringComparison.Ordinal);
        Assert.True(credential.PasswordHash.Length > 25);
    }

    [Fact]
    public async Task LoginBusiness_UsesModernCredentialAfterLegacyMigration()
    {
        var services = CreateDefaultServices();
        var provider = services.BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var store = provider.GetRequiredService<InMemoryDigitalCardsStore>();

        var firstLogin = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));

        Assert.NotNull(firstLogin);

        var original = store.Businesses.Single();
        store.Businesses[0] = new Business(
            original.Id,
            original.Name,
            original.Email,
            "legacy-password-changed",
            original.LogoPath);

        var secondLogin = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));

        Assert.NotNull(secondLogin);
        Assert.Equal(firstLogin!.Id, secondLogin!.Id);
    }

    [Fact]
    public async Task LoginBusiness_WithInvalidLegacyPassword_DoesNotCreateModernCredential()
    {
        var services = CreateDefaultServices();
        var provider = services.BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var credentials = provider.GetRequiredService<IBusinessCredentialRepository>();
        var store = provider.GetRequiredService<InMemoryDigitalCardsStore>();
        var businessId = store.Businesses.Single().Id;

        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "wrong-password"));

        Assert.Null(business);
        Assert.Null(await credentials.FindByBusinessIdAsync(businessId));
    }

    [Fact]
    public void AddInfrastructure_ThrowsWhenAppleWalletProviderIsAppleWithoutConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DigitalCards:AppleWallet:Provider"] = "Apple"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddDigitalCardsInfrastructure(configuration));

        Assert.Contains("DigitalCards:PublicBaseUrl", exception.Message);
        Assert.DoesNotContain(".p12", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddInfrastructure_RegistersAppleWalletWhenProviderIsApple()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DigitalCards:PublicBaseUrl"] = "https://example.test",
                ["DigitalCards:AppleWallet:Provider"] = "Apple",
                ["DigitalCards:AppleWallet:TeamIdentifier"] = "TEAMID1234",
                ["DigitalCards:AppleWallet:PassTypeIdentifier"] = "pass.com.example.digitalcards",
                ["DigitalCards:AppleWallet:OrganizationName"] = "DigitalCards",
                ["DigitalCards:AppleWallet:CertificatePath"] = @"C:\secure\apple-pass-certificate.p12",
                ["DigitalCards:AppleWallet:CertificatePassword"] = "secret",
                ["DigitalCards:AppleWallet:WwdrCertificatePath"] = @"C:\secure\AppleWWDR.cer",
                ["DigitalCards:AppleWallet:AssetsPath"] = @"C:\secure\apple-assets",
                ["DigitalCards:AppleWallet:AuthenticationTokenSecret"] = "this-is-a-long-apple-wallet-test-secret",
                ["DigitalCards:AppleWallet:ApnsBaseUrl"] = "https://api.push.apple.com"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDigitalCardsInfrastructure(configuration);

        var provider = services.BuildServiceProvider();

        Assert.IsType<AppleWalletService>(provider.GetRequiredService<IAppleWalletService>());
        Assert.IsType<AppleWalletPushSender>(provider.GetRequiredService<IAppleWalletPushSender>());
    }

    [Fact]
    public void AddInfrastructure_RegistersRealGoogleWalletWhenProviderIsGoogle()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DigitalCards:GoogleWallet:Provider"] = "Google",
                ["DigitalCards:GoogleWallet:IssuerId"] = "issuer-id",
                ["DigitalCards:GoogleWallet:Origins:0"] = "https://example.test",
                ["DigitalCards:GoogleWallet:CredentialsFilePath"] = @"C:\secure\google-wallet-service-account.json"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDigitalCardsInfrastructure(configuration);

        var provider = services.BuildServiceProvider();

        Assert.IsType<GoogleWalletService>(provider.GetRequiredService<IGoogleWalletService>());
    }

    [Fact]
    public void AddInfrastructure_UseFakeIntegrationsFalseStillEnablesRealGoogleWalletForCompatibility()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DigitalCards:UseFakeIntegrations"] = "false",
                ["DigitalCards:GoogleWallet:IssuerId"] = "issuer-id",
                ["DigitalCards:GoogleWallet:Origins:0"] = "https://example.test",
                ["DigitalCards:GoogleWallet:CredentialsFilePath"] = @"C:\secure\google-wallet-service-account.json"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDigitalCardsInfrastructure(configuration);

        var provider = services.BuildServiceProvider();

        Assert.IsType<GoogleWalletService>(provider.GetRequiredService<IGoogleWalletService>());
        Assert.IsType<FakeWalletEmailOutbox>(provider.GetRequiredService<IEmailSender>());
    }

    [Fact]
    public void AddInfrastructure_RegistersSmtpEmailWhenProviderIsSmtp()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DigitalCards:PublicBaseUrl"] = "https://example.test",
                ["DigitalCards:Email:Provider"] = "Smtp",
                ["DigitalCards:Email:Host"] = "smtp.example.test",
                ["DigitalCards:Email:FromAddress"] = "sender@example.test",
                ["DigitalCards:Email:UserName"] = "sender@example.test",
                ["DigitalCards:Email:Password"] = "secret"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDigitalCardsInfrastructure(configuration);

        var provider = services.BuildServiceProvider();

        Assert.IsType<SmtpEmailSender>(provider.GetRequiredService<IEmailSender>());
        Assert.IsType<FakeWalletEmailOutbox>(provider.GetRequiredService<IWalletEmailOutbox>());
    }

    [Fact]
    public void AddInfrastructure_SmtpEmailRequiresConfigurationBeforeConnecting()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DigitalCards:Email:Provider"] = "Smtp"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddDigitalCardsInfrastructure(configuration));

        Assert.Contains("DigitalCards:Email:Host", exception.Message);
        Assert.DoesNotContain("Password=", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddInfrastructure_SmtpEmailRequiresPublicBaseUrl()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DigitalCards:Email:Provider"] = "Smtp",
                ["DigitalCards:Email:Host"] = "smtp.example.test",
                ["DigitalCards:Email:FromAddress"] = "sender@example.test",
                ["DigitalCards:Email:UserName"] = "sender@example.test",
                ["DigitalCards:Email:Password"] = "secret"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddDigitalCardsInfrastructure(configuration));

        Assert.Contains("DigitalCards:PublicBaseUrl", exception.Message);
        Assert.DoesNotContain("secret", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddInfrastructure_RealGoogleWalletRequiresIssuer()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DigitalCards:UseFakeIntegrations"] = "false",
                ["DigitalCards:GoogleWallet:Origins:0"] = "https://example.test",
                ["DigitalCards:GoogleWallet:CredentialsFilePath"] = @"C:\secure\google-wallet-service-account.json"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddDigitalCardsInfrastructure(configuration));

        Assert.Contains("DigitalCards:GoogleWallet:IssuerId", exception.Message);
    }

    [Fact]
    public void AddInfrastructure_RealGoogleWalletRequiresCredentialsFilePath()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DigitalCards:GoogleWallet:Provider"] = "Google",
                ["DigitalCards:GoogleWallet:IssuerId"] = "issuer-id",
                ["DigitalCards:GoogleWallet:Origins:0"] = "https://example.test"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddDigitalCardsInfrastructure(configuration));

        Assert.Contains("DigitalCards:GoogleWallet:CredentialsFilePath", exception.Message);
    }

    [Fact]
    public async Task EnrollSelectGoogleAndStamp_UsesFakeIntegrationsWithoutProductionServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        services.AddDigitalCardsApplication();
        services.AddDigitalCardsInfrastructure(new ConfigurationBuilder().Build());

        var provider = services.BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();

        var client = await app.RegisterClientAsync(new RegisterClientCommand(
            "maria-test",
            "Maria",
            "Lopez",
            "maria@example.test"));

        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));

        Assert.NotNull(business);

        var enrollment = await app.EnrollClientAsync(new EnrollClientCommand(
            business!.Id,
            client.UserName,
            "http://localhost"));

        Assert.Contains("/Wallet/Select/", enrollment.EnrollmentUrl);
        var publicToken = ExtractWalletToken(enrollment.EnrollmentUrl);
        Assert.NotEqual(enrollment.Card.EnrollmentToken, publicToken);
        Assert.DoesNotContain(enrollment.Card.EnrollmentToken, enrollment.EnrollmentUrl, StringComparison.OrdinalIgnoreCase);

        var outbox = provider.GetRequiredService<IWalletEmailOutbox>();
        var messages = await outbox.ListAsync();

        Assert.Single(messages);
        Assert.Equal("maria@example.test", messages[0].To);
        Assert.Equal(enrollment.EnrollmentUrl, messages[0].EnrollmentUrl);

        var store = provider.GetRequiredService<InMemoryDigitalCardsStore>();
        var tokenRecord = Assert.Single(store.WalletLinkTokens);
        Assert.Equal(enrollment.Card.Id, tokenRecord.CardId);
        Assert.Equal(WalletLinkPurposes.WalletSelect, tokenRecord.Purpose);
        Assert.Equal(64, tokenRecord.TokenHash.Length);
        Assert.DoesNotContain(publicToken, tokenRecord.TokenHash, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(publicToken, tokenRecord.TokenSuffix);

        var landing = await app.GetWalletLandingAsync(publicToken);
        Assert.NotNull(landing);
        Assert.Equal(publicToken, landing!.Token);

        var google = await app.SelectGoogleWalletAsync(publicToken);

        Assert.NotNull(google);
        Assert.StartsWith("fake-google-", google!.ObjectId);

        var stamped = await app.AddStampAsync(new AddStampCommand(business.Id, client.UserName));

        Assert.Equal(2, stamped.CurrentStamps);
        Assert.Equal(2, stamped.LifetimeStamps);
        Assert.NotNull(stamped.GoogleObjectId);
    }

    [Fact]
    public async Task SelectAppleWallet_ReturnsPendingForValidToken()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        services.AddDigitalCardsApplication();
        services.AddDigitalCardsInfrastructure(new ConfigurationBuilder().Build());

        var provider = services.BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();

        var client = await app.RegisterClientAsync(new RegisterClientCommand(
            "apple-test",
            "Ana",
            "Lopez",
            "ana@example.test"));

        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));

        var enrollment = await app.EnrollClientAsync(new EnrollClientCommand(
            business!.Id,
            client.UserName,
            "http://localhost"));

        var publicToken = ExtractWalletToken(enrollment.EnrollmentUrl);
        var result = await app.SelectAppleWalletAsync(publicToken);

        Assert.NotNull(result);
        Assert.Equal(AppleWalletIssueStatus.Pending, result!.Status);
        Assert.Null(result.DownloadUrl);
        Assert.Null(result.SerialNumber);
        Assert.Contains(".pkpass", result.Message);
    }

    [Fact]
    public async Task LegacyCardIdToken_WorksOnlyWhenCompatibilityIsEnabled()
    {
        var compatibleProvider = CreateDefaultServices().BuildServiceProvider();
        var compatibleApp = compatibleProvider.GetRequiredService<DigitalCardsAppService>();
        var compatibleEnrollment = await CreateEnrollmentAsync(compatibleApp, "compat-token");

        var legacyLanding = await compatibleApp.GetWalletLandingAsync(compatibleEnrollment.Card.EnrollmentToken);

        Assert.NotNull(legacyLanding);

        var hardenedServices = CreateDefaultServices(new Dictionary<string, string?>
        {
            ["DigitalCards:WalletLinks:AllowLegacyCardIdTokens"] = "false"
        });
        var hardenedProvider = hardenedServices.BuildServiceProvider();
        var hardenedApp = hardenedProvider.GetRequiredService<DigitalCardsAppService>();
        var hardenedEnrollment = await CreateEnrollmentAsync(hardenedApp, "blocked-token");
        var publicToken = ExtractWalletToken(hardenedEnrollment.EnrollmentUrl);

        Assert.Null(await hardenedApp.GetWalletLandingAsync(hardenedEnrollment.Card.EnrollmentToken));
        Assert.NotNull(await hardenedApp.GetWalletLandingAsync(publicToken));
    }

    [Fact]
    public async Task AddStampToCardAsync_RecordsModernBusinessStampLedger()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));
        var enrollment = await CreateEnrollmentAsync(app, "ledger-ok");

        var detail = await app.AddStampToCardAsync(business!.Id, enrollment.Card.Id);

        Assert.NotNull(detail);
        Assert.Equal(2, detail!.CurrentStamps);
        var ledger = provider.GetRequiredService<IStampLedgerRepository>();
        var records = await ledger.ListRecentByCardIdAsync(enrollment.Card.Id, 5);
        var record = Assert.Single(records);
        Assert.Equal(StampLedgerSource.ModernBusiness, record.Source);
        Assert.Equal(business.Id, record.ActorBusinessId);
        Assert.Equal(1, record.PreviousCheckQTY);
        Assert.Equal(2, record.NewCheckQTY);
        Assert.Equal(1, record.PreviousHistoricCheckQTY);
        Assert.Equal(2, record.NewHistoricCheckQTY);
        Assert.False(record.GoogleWalletAttempted);
        Assert.True(record.AppleWalletAttempted);
        Assert.True(record.AppleWalletSucceeded);
        Assert.Null(record.ErrorSummary);
    }

    [Fact]
    public async Task AddStampToCardAsync_WhenWalletFails_RecordsSafeErrorSummary()
    {
        var services = CreateDefaultServices();
        services.AddScoped<IAppleWalletService, ThrowingAppleWalletService>();
        var provider = services.BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));
        var enrollment = await CreateEnrollmentAsync(app, "ledger-fail");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            app.AddStampToCardAsync(business!.Id, enrollment.Card.Id));

        Assert.Equal("apple push failed with token secret-token", exception.Message);
        var ledger = provider.GetRequiredService<IStampLedgerRepository>();
        var record = Assert.Single(await ledger.ListRecentByCardIdAsync(enrollment.Card.Id, 5));
        Assert.Equal("InvalidOperationException", record.ErrorSummary);
        Assert.DoesNotContain("secret-token", record.ErrorSummary, StringComparison.OrdinalIgnoreCase);
        Assert.True(record.AppleWalletAttempted);
        Assert.False(record.AppleWalletSucceeded);
    }

    [Fact]
    public async Task SelectAppleWallet_ReturnsNullForInvalidToken()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        services.AddDigitalCardsApplication();
        services.AddDigitalCardsInfrastructure(new ConfigurationBuilder().Build());

        var provider = services.BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();

        var result = await app.SelectAppleWalletAsync("missing-token");

        Assert.Null(result);
    }

    private static async Task<EnrollClientResult> CreateEnrollmentAsync(
        DigitalCardsAppService app,
        string userName)
    {
        var client = await app.RegisterClientAsync(new RegisterClientCommand(
            userName,
            "Token",
            "User",
            $"{userName}@example.test"));

        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));

        return await app.EnrollClientAsync(new EnrollClientCommand(
            business!.Id,
            client.UserName,
            "http://localhost"));
    }

    private static string ExtractWalletToken(string enrollmentUrl)
    {
        const string marker = "/Wallet/Select/";
        var index = enrollmentUrl.IndexOf(marker, StringComparison.Ordinal);
        return index < 0
            ? throw new InvalidOperationException("Wallet link was not found.")
            : enrollmentUrl[(index + marker.Length)..];
    }

    private static ServiceCollection CreateDefaultServices(IReadOnlyDictionary<string, string?>? configurationValues = null)
    {
        var configurationBuilder = new ConfigurationBuilder();
        if (configurationValues is not null)
        {
            configurationBuilder.AddInMemoryCollection(configurationValues);
        }

        var configuration = configurationBuilder.Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddDigitalCardsApplication();
        services.AddDigitalCardsInfrastructure(configuration);
        return services;
    }

    private sealed class ThrowingAppleWalletService : IAppleWalletService
    {
        public Task<AppleWalletIssueResult> IssueAsync(
            LoyaltyCard card,
            Client client,
            Business business,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AppleWalletIssueResult(
                AppleWalletIssueStatus.Pending,
                "Pending",
                DownloadUrl: null,
                SerialNumber: null));
        }

        public Task<AppleWalletPassFile> CreatePassAsync(
            LoyaltyCard card,
            Client client,
            Business business,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AppleWalletPassRequestResult> CreateUpdatedPassAsync(
            string passTypeIdentifier,
            string serialNumber,
            string? authorizationHeader,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AppleWalletRegistrationStatus> RegisterDeviceAsync(
            string deviceLibraryIdentifier,
            string passTypeIdentifier,
            string serialNumber,
            string pushToken,
            string? authorizationHeader,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AppleWalletUnregistrationStatus> UnregisterDeviceAsync(
            string deviceLibraryIdentifier,
            string passTypeIdentifier,
            string serialNumber,
            string? authorizationHeader,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AppleWalletUpdatedPasses?> ListUpdatedPassesAsync(
            string deviceLibraryIdentifier,
            string passTypeIdentifier,
            string? previousLastUpdated,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task NotifyPassUpdatedAsync(
            LoyaltyCard card,
            Client client,
            Business business,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("apple push failed with token secret-token");
        }
    }
}
