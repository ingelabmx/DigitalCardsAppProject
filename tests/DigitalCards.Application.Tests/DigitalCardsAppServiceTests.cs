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
using System.Security.Cryptography;
using System.Text;

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
        Assert.IsType<MySqlClientCredentialRepository>(provider.GetRequiredService<IClientCredentialRepository>());
        Assert.IsType<MySqlClientConsentRepository>(provider.GetRequiredService<IClientConsentRepository>());
        Assert.IsType<MySqlBusinessRepository>(provider.GetRequiredService<IBusinessRepository>());
        Assert.IsType<MySqlBusinessBrandingRepository>(provider.GetRequiredService<IBusinessBrandingRepository>());
        Assert.IsType<EmailTemplateRenderer>(provider.GetRequiredService<IEmailTemplateRenderer>());
        Assert.IsType<MySqlAdminUserRepository>(provider.GetRequiredService<IAdminUserRepository>());
        Assert.IsType<MySqlAdminCredentialRepository>(provider.GetRequiredService<IAdminCredentialRepository>());
        Assert.IsType<MySqlAuditEventRepository>(provider.GetRequiredService<IAuditEventRepository>());
        Assert.IsType<MySqlCutoverSmokeRepository>(provider.GetRequiredService<ICutoverSmokeRepository>());
        Assert.IsType<MySqlBusinessCredentialRepository>(provider.GetRequiredService<IBusinessCredentialRepository>());
        Assert.IsType<MySqlLoyaltyCardRepository>(provider.GetRequiredService<ILoyaltyCardRepository>());
        Assert.IsType<MySqlAppleWalletPassRepository>(provider.GetRequiredService<IAppleWalletPassRepository>());
        Assert.IsType<MySqlWalletLinkTokenRepository>(provider.GetRequiredService<IWalletLinkTokenRepository>());
        Assert.IsType<MySqlPasswordResetTokenRepository>(provider.GetRequiredService<IPasswordResetTokenRepository>());
        Assert.IsType<MySqlStampLedgerRepository>(provider.GetRequiredService<IStampLedgerRepository>());
        Assert.IsType<MySqlPilotBusinessRepository>(provider.GetRequiredService<IPilotBusinessRepository>());
        Assert.IsType<MySqlPilotClientRepository>(provider.GetRequiredService<IPilotClientRepository>());
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
        Assert.IsType<EmailTemplateRenderer>(provider.GetRequiredService<IEmailTemplateRenderer>());
        Assert.IsType<InMemoryAdminUserRepository>(provider.GetRequiredService<IAdminUserRepository>());
        Assert.IsType<InMemoryAdminCredentialRepository>(provider.GetRequiredService<IAdminCredentialRepository>());
        Assert.IsType<InMemoryAuditEventRepository>(provider.GetRequiredService<IAuditEventRepository>());
        Assert.IsType<InMemoryCutoverSmokeRepository>(provider.GetRequiredService<ICutoverSmokeRepository>());
        Assert.IsType<InMemoryClientConsentRepository>(provider.GetRequiredService<IClientConsentRepository>());
        Assert.IsType<InMemoryClientCredentialRepository>(provider.GetRequiredService<IClientCredentialRepository>());
        Assert.IsType<InMemoryBusinessBrandingRepository>(provider.GetRequiredService<IBusinessBrandingRepository>());
        Assert.IsType<InMemoryBusinessCredentialRepository>(provider.GetRequiredService<IBusinessCredentialRepository>());
        Assert.IsType<InMemoryWalletLinkTokenRepository>(provider.GetRequiredService<IWalletLinkTokenRepository>());
        Assert.IsType<InMemoryPasswordResetTokenRepository>(provider.GetRequiredService<IPasswordResetTokenRepository>());
        Assert.IsType<InMemoryStampLedgerRepository>(provider.GetRequiredService<IStampLedgerRepository>());
        Assert.IsType<InMemoryPilotBusinessRepository>(provider.GetRequiredService<IPilotBusinessRepository>());
        Assert.IsType<InMemoryPilotClientRepository>(provider.GetRequiredService<IPilotClientRepository>());
        Assert.IsType<FakeWalletEmailOutbox>(provider.GetRequiredService<IPasswordResetEmailOutbox>());
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
    public async Task LoginAdmin_WithLegacyPassword_CreatesModernCredential()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var adminApp = provider.GetRequiredService<AdminAppService>();
        var credentials = provider.GetRequiredService<IAdminCredentialRepository>();

        var admin = await adminApp.LoginAdminAsync(new AdminLoginCommand(
            "admin@digitalcards.test",
            "admin123"));

        Assert.NotNull(admin);
        var credential = await credentials.FindByAdminUserIdAsync(admin!.Id);
        Assert.NotNull(credential);
        Assert.True(credential!.PasswordHash.Length > 25);
        Assert.DoesNotContain("admin123", credential.PasswordHash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoginAdmin_UsesModernCredentialAfterLegacyMigration()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var adminApp = provider.GetRequiredService<AdminAppService>();
        var store = provider.GetRequiredService<InMemoryDigitalCardsStore>();

        var firstLogin = await adminApp.LoginAdminAsync(new AdminLoginCommand(
            "DCAdmin",
            "admin123"));

        Assert.NotNull(firstLogin);

        var original = store.AdminUsers.Single();
        store.AdminUsers[0] = new AdminUser(
            original.Id,
            original.UserName,
            original.FirstName,
            original.LastName,
            original.Email,
            "legacy-password-changed");

        var secondLogin = await adminApp.LoginAdminAsync(new AdminLoginCommand(
            "DCAdmin",
            "admin123"));

        Assert.NotNull(secondLogin);
        Assert.Equal(original.Id, secondLogin!.Id);
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
    public async Task CreateAdminAsync_CreatesLegacyAdminAndModernCredential()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var adminApp = provider.GetRequiredService<AdminAppService>();
        var admins = provider.GetRequiredService<IAdminUserRepository>();
        var credentials = provider.GetRequiredService<IAdminCredentialRepository>();
        var audit = provider.GetRequiredService<IAuditEventRepository>();
        var actingAdmin = await adminApp.LoginAdminAsync(new AdminLoginCommand(
            "DCAdmin",
            "admin123"));
        const string password = "NewAdmin123!";

        var result = await adminApp.CreateAdminAsync(new CreateAdminCommand(
            "OpsAdmin",
            "Ops",
            "Admin",
            "ops@example.test",
            password,
            actingAdmin!.Id));

        Assert.True(result.Succeeded);
        Assert.Equal("OpsAdmin", result.Admin!.UserName);
        Assert.Equal("ops@example.test", result.Admin.Email);

        var admin = await admins.FindByUserNameOrEmailAsync("OpsAdmin");
        Assert.NotNull(admin);
        Assert.Equal(25, admin!.PasswordHashPlaceholder.Length);
        Assert.Equal(ExpectedLegacyBusinessPasswordHash(password), admin.PasswordHashPlaceholder);
        Assert.DoesNotContain(password, admin.PasswordHashPlaceholder, StringComparison.Ordinal);

        var credential = await credentials.FindByAdminUserIdAsync(admin.Id);
        Assert.NotNull(credential);
        Assert.DoesNotContain(password, credential!.PasswordHash, StringComparison.Ordinal);

        var login = await adminApp.LoginAdminAsync(new AdminLoginCommand("OpsAdmin", password));
        Assert.NotNull(login);

        var auditEvents = await audit.ListRecentAsync(
            OperationalAuditEventType.AdminCreated,
            search: null,
            from: null,
            to: null,
            limit: 10);
        var auditEvent = Assert.Single(auditEvents, item => item.TargetAdminUserId == admin.Id);
        Assert.Equal(actingAdmin.Id, auditEvent.ActorAdminUserId);
        Assert.DoesNotContain(password, auditEvent.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateAdminAsync_WhenDuplicateUserNameOrEmail_ReturnsSafeError()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var adminApp = provider.GetRequiredService<AdminAppService>();
        var actingAdmin = await adminApp.LoginAdminAsync(new AdminLoginCommand(
            "DCAdmin",
            "admin123"));

        var duplicateUserName = await adminApp.CreateAdminAsync(new CreateAdminCommand(
            "DCAdmin",
            "Unique",
            "Admin",
            "unique@example.test",
            "NewAdmin123!",
            actingAdmin!.Id));
        var duplicateEmail = await adminApp.CreateAdminAsync(new CreateAdminCommand(
            "UniqueAdmin",
            "Unique",
            "Admin",
            "admin@digitalcards.test",
            "NewAdmin123!",
            actingAdmin.Id));

        Assert.False(duplicateUserName.Succeeded);
        Assert.Equal("Ya existe un admin con ese usuario o correo.", duplicateUserName.ErrorMessage);
        Assert.False(duplicateEmail.Succeeded);
        Assert.Equal("Ya existe un admin con ese usuario o correo.", duplicateEmail.ErrorMessage);
        Assert.DoesNotContain("password", duplicateUserName.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", duplicateEmail.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResetAdminPasswordAsync_UpdatesLegacyAndModernCredentials()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var adminApp = provider.GetRequiredService<AdminAppService>();
        var admins = provider.GetRequiredService<IAdminUserRepository>();
        var credentials = provider.GetRequiredService<IAdminCredentialRepository>();
        var actingAdmin = await adminApp.LoginAdminAsync(new AdminLoginCommand(
            "DCAdmin",
            "admin123"));
        const string oldPassword = "NewAdmin123!";
        const string newPassword = "ChangedAdmin123!";
        var created = await adminApp.CreateAdminAsync(new CreateAdminCommand(
            "ResetAdmin",
            "Reset",
            "Admin",
            "reset@example.test",
            oldPassword,
            actingAdmin!.Id));

        var result = await adminApp.ResetAdminPasswordAsync(new ResetAdminPasswordCommand(
            created.Admin!.Id,
            actingAdmin.Id,
            newPassword));

        Assert.True(result.Succeeded);
        var admin = await admins.FindByIdAsync(created.Admin.Id);
        Assert.NotNull(admin);
        Assert.Equal(ExpectedLegacyBusinessPasswordHash(newPassword), admin!.PasswordHashPlaceholder);
        Assert.DoesNotContain(newPassword, admin.PasswordHashPlaceholder, StringComparison.Ordinal);
        var credential = await credentials.FindByAdminUserIdAsync(admin.Id);
        Assert.NotNull(credential);
        Assert.DoesNotContain(newPassword, credential!.PasswordHash, StringComparison.Ordinal);

        Assert.Null(await adminApp.LoginAdminAsync(new AdminLoginCommand("ResetAdmin", oldPassword)));
        Assert.NotNull(await adminApp.LoginAdminAsync(new AdminLoginCommand("ResetAdmin", newPassword)));
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
        Assert.Equal(BusinessActivationStatus.ModernPrimary, enabled.ActivationStatus);
        Assert.NotNull(disabled);
        Assert.False(disabled!.IsEnabled);
        Assert.Equal(BusinessActivationStatus.Inactive, disabled.ActivationStatus);
        var listed = Assert.Single(businesses);
        Assert.False(listed.IsEnabled);
        Assert.Equal(BusinessActivationStatus.Inactive, listed.ActivationStatus);
        Assert.Equal("pausado", listed.Notes);
    }

    [Fact]
    public async Task SetPilotClientAsync_UpsertsPilotState()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var adminApp = provider.GetRequiredService<AdminAppService>();
        var admin = await adminApp.LoginAdminAsync(new AdminLoginCommand(
            "DCAdmin",
            "admin123"));
        var client = await app.RegisterClientAsync(new RegisterClientCommand(
            "pilotclient1",
            "Pilot",
            "Client",
            "pilotclient1@example.test"));

        var enabled = await adminApp.SetPilotClientAsync(new SetPilotClientCommand(
            client.Id,
            admin!.Id,
            IsEnabled: true,
            Notes: "cliente piloto"));
        var disabled = await adminApp.SetPilotClientAsync(new SetPilotClientCommand(
            client.Id,
            admin.Id,
            IsEnabled: false,
            Notes: "pausado"));
        var clients = await adminApp.ListPilotClientsAsync("pilotclient1");

        Assert.NotNull(enabled);
        Assert.True(enabled!.IsEnabled);
        Assert.NotNull(disabled);
        Assert.False(disabled!.IsEnabled);
        var listed = Assert.Single(clients);
        Assert.Equal(client.Id, listed.ClientId);
        Assert.Equal("pilotclient1", listed.UserName);
        Assert.False(listed.IsEnabled);
        Assert.Equal("pausado", listed.Notes);
    }

    [Fact]
    public async Task SearchSupportAsync_ReturnsSafeCardWalletAndLedgerState()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var adminApp = provider.GetRequiredService<AdminAppService>();
        var applePasses = provider.GetRequiredService<IAppleWalletPassRepository>();
        var client = await app.RegisterClientAsync(new RegisterClientCommand(
            "supportclient1",
            "Support",
            "Client",
            "supportclient1@example.test"));
        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));
        var enrollment = await app.EnrollClientAsync(new EnrollClientCommand(
            business!.Id,
            client.UserName,
            "https://app.puntelio.com"));
        var publicToken = ExtractWalletToken(enrollment.EnrollmentUrl);
        await app.SelectGoogleWalletAsync(publicToken);
        await applePasses.UpsertPassAsync(new AppleWalletPassRecord(
            "pass.com.puntelio.loyalty",
            $"serial-{enrollment.Card.Id:N}",
            enrollment.Card.Id,
            "auth-token-secret-hash",
            "42",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow));
        await applePasses.UpsertDeviceAsync(new AppleWalletDeviceRecord(
            "device-library-secret",
            "push-token-secret",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow));
        await applePasses.AddRegistrationAsync(
            "device-library-secret",
            "pass.com.puntelio.loyalty",
            $"serial-{enrollment.Card.Id:N}",
            DateTimeOffset.UtcNow);
        await app.AddStampToCardAsync(business.Id, enrollment.Card.Id);

        var result = await adminApp.SearchSupportAsync(new AdminSupportQuery("supportclient1"));

        var card = Assert.Single(result.Cards);
        Assert.Equal(enrollment.Card.Id, card.CardId);
        Assert.Equal("supportclient1", card.Client.UserName);
        Assert.Equal("Demo Coffee", card.Business.BusinessName);
        Assert.True(card.GoogleIssued);
        Assert.NotNull(card.GoogleObjectSuffix);
        Assert.True(card.AppleTracked);
        Assert.Equal(1, card.AppleRegisteredDeviceCount);
        Assert.Equal("42", card.AppleUpdateTag);
        Assert.DoesNotContain("auth-token-secret-hash", string.Join(' ', card.AppleSerialSuffix, card.GoogleObjectSuffix), StringComparison.OrdinalIgnoreCase);
        var ledger = Assert.Single(card.RecentStampEvents);
        Assert.Equal(StampLedgerSource.ModernBusiness, ledger.Source);
        Assert.True(ledger.GoogleWalletAttempted);
        Assert.True(ledger.GoogleWalletSucceeded);
        Assert.True(ledger.AppleWalletAttempted);
        Assert.True(ledger.AppleWalletSucceeded);
    }

    [Fact]
    public async Task RetryWalletUpdateAsync_RecordsAdminRetryForTrackedWallets()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var adminApp = provider.GetRequiredService<AdminAppService>();
        var applePasses = provider.GetRequiredService<IAppleWalletPassRepository>();
        var admin = await adminApp.LoginAdminAsync(new AdminLoginCommand(
            "DCAdmin",
            "admin123"));
        var enrollment = await CreateEnrollmentAsync(app, "retryok");
        await app.SelectGoogleWalletAsync(ExtractWalletToken(enrollment.EnrollmentUrl));
        await applePasses.UpsertPassAsync(new AppleWalletPassRecord(
            "pass.com.puntelio.loyalty",
            $"serial-{enrollment.Card.Id:N}",
            enrollment.Card.Id,
            "auth-token-secret-hash",
            "42",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow));

        var result = await adminApp.RetryWalletUpdateAsync(new AdminWalletRetryCommand(
            enrollment.Card.Id,
            admin!.Id));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Card);
        var ledger = provider.GetRequiredService<IStampLedgerRepository>();
        var record = Assert.Single((await ledger.ListRecentByCardIdAsync(enrollment.Card.Id, 5))
            .Where(item => item.Source == StampLedgerSource.AdminRetry));
        Assert.Equal(1, record.PreviousCheckQTY);
        Assert.Equal(1, record.NewCheckQTY);
        Assert.True(record.GoogleWalletAttempted);
        Assert.True(record.GoogleWalletSucceeded);
        Assert.True(record.AppleWalletAttempted);
        Assert.True(record.AppleWalletSucceeded);
        Assert.Null(record.ErrorSummary);
    }

    [Fact]
    public async Task RetryWalletUpdateAsync_WhenWalletFails_RecordsSafeError()
    {
        var services = CreateDefaultServices();
        services.AddScoped<IAppleWalletService, ThrowingAppleWalletService>();
        var provider = services.BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var adminApp = provider.GetRequiredService<AdminAppService>();
        var applePasses = provider.GetRequiredService<IAppleWalletPassRepository>();
        var admin = await adminApp.LoginAdminAsync(new AdminLoginCommand(
            "DCAdmin",
            "admin123"));
        var enrollment = await CreateEnrollmentAsync(app, "retryfail");
        await applePasses.UpsertPassAsync(new AppleWalletPassRecord(
            "pass.com.puntelio.loyalty",
            $"serial-{enrollment.Card.Id:N}",
            enrollment.Card.Id,
            "auth-token-secret-hash",
            "42",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow));

        var result = await adminApp.RetryWalletUpdateAsync(new AdminWalletRetryCommand(
            enrollment.Card.Id,
            admin!.Id));

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Card);
        Assert.Equal("El reintento termino con alertas seguras. Revisa StampLedger.", result.ErrorMessage);
        var ledger = provider.GetRequiredService<IStampLedgerRepository>();
        var record = Assert.Single((await ledger.ListRecentByCardIdAsync(enrollment.Card.Id, 5))
            .Where(item => item.Source == StampLedgerSource.AdminRetry));
        Assert.True(record.AppleWalletAttempted);
        Assert.False(record.AppleWalletSucceeded);
        Assert.Equal("InvalidOperationException", record.ErrorSummary);
        Assert.DoesNotContain("secret-token", record.ErrorSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RefreshBusinessWalletBrandingAsync_RecordsBrandingRefreshForTrackedWallets()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var applePasses = provider.GetRequiredService<IAppleWalletPassRepository>();
        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));
        var enrollment = await CreateEnrollmentAsync(app, "brandrefresh");
        await app.SelectGoogleWalletAsync(ExtractWalletToken(enrollment.EnrollmentUrl));
        await applePasses.UpsertPassAsync(new AppleWalletPassRecord(
            "pass.com.puntelio.loyalty",
            $"serial-{enrollment.Card.Id:N}",
            enrollment.Card.Id,
            "auth-token-secret-hash",
            "42",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow));

        var result = await app.RefreshBusinessWalletBrandingAsync(
            new WalletBrandingRefreshCommand(business!.Id, Limit: 10));

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.CardsWithTrackedWallets);
        Assert.Equal(1, result.GoogleWalletAttempted);
        Assert.Equal(1, result.GoogleWalletSucceeded);
        Assert.Equal(1, result.AppleWalletAttempted);
        Assert.Equal(1, result.AppleWalletSucceeded);

        var ledger = provider.GetRequiredService<IStampLedgerRepository>();
        var record = Assert.Single((await ledger.ListRecentByCardIdAsync(enrollment.Card.Id, 5))
            .Where(item => item.Source == StampLedgerSource.BrandingRefresh));
        Assert.Equal(business.Id, record.ActorBusinessId);
        Assert.Equal(record.PreviousCheckQTY, record.NewCheckQTY);
        Assert.Equal(record.PreviousHistoricCheckQTY, record.NewHistoricCheckQTY);
        Assert.True(record.GoogleWalletAttempted);
        Assert.True(record.GoogleWalletSucceeded);
        Assert.True(record.AppleWalletAttempted);
        Assert.True(record.AppleWalletSucceeded);
        Assert.Null(record.ErrorSummary);
    }

    [Fact]
    public async Task RefreshBusinessWalletBrandingAsync_WithFullRefreshProcessesMoreThanSearchLimit()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));

        for (var index = 0; index < 105; index++)
        {
            var enrollment = await CreateEnrollmentAsync(app, $"refreshall{index:D3}");
            await app.SelectGoogleWalletAsync(ExtractWalletToken(enrollment.EnrollmentUrl));
        }

        var result = await app.RefreshBusinessWalletBrandingAsync(
            new WalletBrandingRefreshCommand(business!.Id, Limit: 0));

        Assert.True(result.Succeeded);
        Assert.Equal(105, result.CardsScanned);
        Assert.Equal(105, result.CardsWithTrackedWallets);
        Assert.Equal(105, result.GoogleWalletAttempted);
        Assert.Equal(105, result.GoogleWalletSucceeded);

        var store = provider.GetRequiredService<InMemoryDigitalCardsStore>();
        Assert.Equal(105, store.StampLedger.Count(item => item.Source == StampLedgerSource.BrandingRefresh));
    }

    [Fact]
    public async Task AdminRefreshBusinessWalletBrandingAsync_RecordsAuditEvent()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var adminApp = provider.GetRequiredService<AdminAppService>();
        var admin = await adminApp.LoginAdminAsync(new AdminLoginCommand(
            "DCAdmin",
            "admin123"));
        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));
        var enrollment = await CreateEnrollmentAsync(app, "adminbrandrefresh");
        await app.SelectGoogleWalletAsync(ExtractWalletToken(enrollment.EnrollmentUrl));

        var result = await adminApp.RefreshBusinessWalletBrandingAsync(
            new AdminWalletBrandingRefreshCommand(business!.Id, admin!.Id, Limit: 10));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Refresh);
        Assert.Equal(1, result.Refresh.GoogleWalletSucceeded);
        var store = provider.GetRequiredService<InMemoryDigitalCardsStore>();
        Assert.Contains(store.AuditEvents, item =>
            item.EventType == OperationalAuditEventType.WalletBrandingRefreshRequested &&
            item.BusinessId == business.Id &&
            item.ActorAdminUserId == admin.Id);
    }

    [Fact]
    public async Task GetReportsAsync_ReturnsSafeOperationalSummary()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var adminApp = provider.GetRequiredService<AdminAppService>();
        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));
        var client = await app.RegisterClientAsync(new RegisterClientCommand(
            "reportclient1",
            "Report",
            "Client",
            "reportclient1@example.test",
            "ClientPass123!"));
        var enrollment = await app.EnrollClientAsync(new EnrollClientCommand(
            business!.Id,
            client.UserName,
            "https://app.puntelio.com"));
        await app.SelectGoogleWalletAsync(ExtractWalletToken(enrollment.EnrollmentUrl));
        await app.AddStampToCardAsync(business.Id, enrollment.Card.Id);

        var reports = await adminApp.GetReportsAsync();

        Assert.True(reports.BusinessCount >= 1);
        Assert.True(reports.CardCount >= 1);
        Assert.True(reports.ClientCount >= 1);
        Assert.True(reports.CurrentStampTotal >= 2);
        Assert.True(reports.GoogleIssuedCount >= 1);
        var reportBusiness = Assert.Single(reports.Businesses.Where(item => item.BusinessName == "Demo Coffee"));
        Assert.True(reportBusiness.CardCount >= 1);
        Assert.True(reportBusiness.ClientCount >= 1);
        Assert.Contains(reports.RecentCards, card => card.ClientUserName == "reportclient1");
        Assert.DoesNotContain("password", string.Join(' ', reports.Businesses.Select(item => item.BusinessEmail)), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListPilotClientsAsync_SearchesLegacyClientsWithoutPasswordData()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var adminApp = provider.GetRequiredService<AdminAppService>();
        await app.RegisterClientAsync(new RegisterClientCommand(
            "lookupclient1",
            "Lookup",
            "Client",
            "lookupclient1@example.test"));

        var clients = await adminApp.ListPilotClientsAsync("lookupclient1");

        var client = Assert.Single(clients);
        Assert.Equal("lookupclient1", client.UserName);
        Assert.Equal("lookupclient1@example.test", client.ClientEmail);
        Assert.False(client.IsEnabled);
        Assert.DoesNotContain("password", string.Join(' ', client.UserName, client.ClientName, client.ClientEmail), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash", string.Join(' ', client.UserName, client.ClientName, client.ClientEmail), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListClientConsoleAsync_ReturnsLinkedBusinessesCardsAndStampData()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var adminApp = provider.GetRequiredService<AdminAppService>();
        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));
        var client = await app.RegisterClientAsync(new RegisterClientCommand(
            "consoleclient1",
            "Console",
            "Client",
            "consoleclient1@example.test"));
        var enrollment = await app.EnrollClientAsync(new EnrollClientCommand(
            business!.Id,
            client.UserName,
            "https://app.puntelio.com"));
        await app.AddStampToCardAsync(business.Id, enrollment.Card.Id);

        var clients = await adminApp.ListClientConsoleAsync("consoleclient1");

        var console = Assert.Single(clients);
        Assert.Equal("consoleclient1", console.UserName);
        Assert.Equal("Console Client", console.ClientName);
        Assert.Equal(1, console.CardCount);
        Assert.Equal(2, console.CurrentStamps);
        Assert.Equal(2, console.LifetimeStamps);
        var card = Assert.Single(console.Cards);
        Assert.Equal("Demo Coffee", card.BusinessName);
        Assert.Equal(2, card.CurrentStamps);
        Assert.Equal(2, card.LifetimeStamps);
        Assert.Equal("Pendiente", card.CardStatus);
        Assert.DoesNotContain("password", string.Join(' ', console.UserName, console.ClientName, console.ClientEmail), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash", string.Join(' ', console.UserName, console.ClientName, console.ClientEmail), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateBusinessAsync_CreatesLegacyBusinessModernCredentialAndPilotAccess()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var adminApp = provider.GetRequiredService<AdminAppService>();
        var businesses = provider.GetRequiredService<IBusinessRepository>();
        var credentials = provider.GetRequiredService<IBusinessCredentialRepository>();
        var pilotBusinesses = provider.GetRequiredService<IPilotBusinessRepository>();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var admin = await adminApp.LoginAdminAsync(new AdminLoginCommand(
            "DCAdmin",
            "admin123"));
        const string password = "startpass1";

        var result = await adminApp.CreateBusinessAsync(new CreateBusinessCommand(
            "Fresh Cafe",
            "fresh@example.test",
            password,
            admin!.Id,
            EnablePilot: true,
            Notes: "piloto creado desde admin"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Business);
        Assert.Equal("Fresh Cafe", result.Business!.BusinessName);
        Assert.Equal("fresh@example.test", result.Business.BusinessEmail);
        Assert.True(result.Business.IsEnabled);

        var business = await businesses.FindByEmailAsync("fresh@example.test");
        Assert.NotNull(business);
        Assert.Equal("/img/demo-coffee.svg", business!.LogoPath);
        Assert.Equal(25, business.PasswordHashPlaceholder.Length);
        Assert.Equal(ExpectedLegacyBusinessPasswordHash(password), business.PasswordHashPlaceholder);
        Assert.DoesNotContain(password, business.PasswordHashPlaceholder, StringComparison.Ordinal);

        var credential = await credentials.FindByBusinessIdAsync(business.Id);
        Assert.NotNull(credential);
        Assert.True(credential!.PasswordHash.Length > 25);
        Assert.DoesNotContain(password, credential.PasswordHash, StringComparison.Ordinal);

        var pilot = await pilotBusinesses.FindByBusinessIdAsync(business.Id);
        Assert.NotNull(pilot);
        Assert.True(pilot!.IsEnabled);
        Assert.Equal(admin.Id, pilot.UpdatedByAdminUserId);

        var login = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "fresh@example.test",
            password));
        Assert.NotNull(login);
        Assert.Equal(business.Id, login!.Id);
    }

    [Fact]
    public async Task CreateBusinessAsync_WhenDuplicateNameOrEmail_ReturnsSafeError()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var adminApp = provider.GetRequiredService<AdminAppService>();
        var admin = await adminApp.LoginAdminAsync(new AdminLoginCommand(
            "DCAdmin",
            "admin123"));

        var duplicateName = await adminApp.CreateBusinessAsync(new CreateBusinessCommand(
            "Demo Coffee",
            "unique@example.test",
            "startpass1",
            admin!.Id,
            EnablePilot: false,
            Notes: null));
        var duplicateEmail = await adminApp.CreateBusinessAsync(new CreateBusinessCommand(
            "Unique Cafe",
            "demo@digitalcards.test",
            "startpass1",
            admin.Id,
            EnablePilot: false,
            Notes: null));

        Assert.False(duplicateName.Succeeded);
        Assert.Equal("Ya existe un negocio con ese nombre.", duplicateName.ErrorMessage);
        Assert.False(duplicateEmail.Succeeded);
        Assert.Equal("Ya existe un negocio con ese correo.", duplicateEmail.ErrorMessage);
        Assert.DoesNotContain("password", duplicateName.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", duplicateEmail.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateBusinessAsync_WithoutPilot_DoesNotEnablePilotBusiness()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var adminApp = provider.GetRequiredService<AdminAppService>();
        var pilotBusinesses = provider.GetRequiredService<IPilotBusinessRepository>();
        var admin = await adminApp.LoginAdminAsync(new AdminLoginCommand(
            "DCAdmin",
            "admin123"));

        var result = await adminApp.CreateBusinessAsync(new CreateBusinessCommand(
            "Quiet Cafe",
            "quiet@example.test",
            "startpass1",
            admin!.Id,
            EnablePilot: false,
            Notes: null));

        Assert.True(result.Succeeded);
        Assert.False(result.Business!.IsEnabled);
        Assert.Equal(BusinessActivationStatus.Inactive, result.Business.ActivationStatus);
        Assert.Null(await pilotBusinesses.FindByBusinessIdAsync(result.Business.BusinessId));
    }

    [Fact]
    public async Task UpdateBusinessProfileAsync_UpdatesBusinessFieldsAndPilotState()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var adminApp = provider.GetRequiredService<AdminAppService>();
        var businesses = provider.GetRequiredService<IBusinessRepository>();
        var pilotBusinesses = provider.GetRequiredService<IPilotBusinessRepository>();
        var admin = await adminApp.LoginAdminAsync(new AdminLoginCommand(
            "DCAdmin",
            "admin123"));
        var created = await adminApp.CreateBusinessAsync(new CreateBusinessCommand(
            "Edit Cafe",
            "edit@example.test",
            "startpass1",
            admin!.Id,
            EnablePilot: false,
            Notes: null));

        var result = await adminApp.UpdateBusinessProfileAsync(new UpdateBusinessProfileCommand(
            created.Business!.BusinessId,
            admin.Id,
            "Edited Cafe",
            "edited@example.test",
            "~/Logos/edited.png",
            IsPilotEnabled: true,
            Notes: "habilitado desde profile",
            ActivationStatus: BusinessActivationStatus.ModernPrimary));

        Assert.True(result.Succeeded);
        Assert.Equal("Edited Cafe", result.Business!.BusinessName);
        Assert.Equal("edited@example.test", result.Business.BusinessEmail);
        Assert.Equal("~/Logos/edited.png", result.Business.BusinessLogo);
        Assert.True(result.Business.IsPilotEnabled);
        Assert.Equal(BusinessActivationStatus.ModernPrimary, result.Business.ActivationStatus);
        Assert.Equal("habilitado desde profile", result.Business.Notes);

        var business = await businesses.FindByIdAsync(created.Business.BusinessId);
        Assert.NotNull(business);
        Assert.Equal("Edited Cafe", business!.Name);
        Assert.Equal("edited@example.test", business.Email);
        Assert.Equal("~/Logos/edited.png", business.LogoPath);
        var pilot = await pilotBusinesses.FindByBusinessIdAsync(business.Id);
        Assert.NotNull(pilot);
        Assert.True(pilot!.IsEnabled);
        Assert.Equal(BusinessActivationStatus.ModernPrimary, pilot.ActivationStatus);
    }

    [Fact]
    public async Task UpdateBusinessProfileAsync_InactiveStatusDisablesModernAccess()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var adminApp = provider.GetRequiredService<AdminAppService>();
        var pilotBusinesses = provider.GetRequiredService<IPilotBusinessRepository>();
        var admin = await adminApp.LoginAdminAsync(new AdminLoginCommand(
            "DCAdmin",
            "admin123"));
        var created = await adminApp.CreateBusinessAsync(new CreateBusinessCommand(
            "Inactive Cafe",
            "inactive@example.test",
            "startpass1",
            admin!.Id,
            EnablePilot: true,
            Notes: null));

        var result = await adminApp.UpdateBusinessProfileAsync(new UpdateBusinessProfileCommand(
            created.Business!.BusinessId,
            admin.Id,
            "Inactive Cafe",
            "inactive@example.test",
            "/img/demo-coffee.svg",
            IsPilotEnabled: true,
            Notes: "pausado por admin",
            ActivationStatus: BusinessActivationStatus.Inactive));

        Assert.True(result.Succeeded);
        Assert.False(result.Business!.IsPilotEnabled);
        Assert.Equal(BusinessActivationStatus.Inactive, result.Business.ActivationStatus);
        var access = await pilotBusinesses.FindByBusinessIdAsync(created.Business.BusinessId);
        Assert.NotNull(access);
        Assert.False(access!.IsEnabled);
        Assert.Equal(BusinessActivationStatus.Inactive, access.ActivationStatus);
    }

    [Fact]
    public async Task RecordCutoverSmokeEvidenceAsync_StoresLatestEvidenceAndAuditEvent()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var adminApp = provider.GetRequiredService<AdminAppService>();
        var audit = provider.GetRequiredService<IAuditEventRepository>();
        var admin = await adminApp.LoginAdminAsync(new AdminLoginCommand(
            "DCAdmin",
            "admin123"));
        var businesses = provider.GetRequiredService<IBusinessRepository>();
        var business = await businesses.FindByEmailAsync("demo@digitalcards.test");

        var result = await adminApp.RecordCutoverSmokeEvidenceAsync(
            new RecordCutoverSmokeEvidenceCommand(
                business!.Id,
                admin!.Id,
                HealthOk: true,
                ReadyOk: true,
                EmailOk: true,
                AppleWalletOk: true,
                GoogleWalletOk: true,
                ModernStampOk: true,
                SupportReviewed: true,
                Notes: "smoke real validado"));

        Assert.True(result.Succeeded);
        Assert.True(result.Evidence!.IsComplete);
        Assert.Equal("smoke real validado", result.Evidence.Notes);
        Assert.Equal("DCAdmin", result.Evidence.AdminUserName);

        var latest = await adminApp.ListCutoverSmokeEvidenceAsync(business.Id);
        Assert.Single(latest);
        Assert.True(latest[0].IsComplete);

        var auditRecords = await audit.ListRecentAsync(
            OperationalAuditEventType.CutoverSmokeEvidenceRecorded,
            search: null,
            from: null,
            to: null,
            limit: 10);
        Assert.Contains(auditRecords, item =>
            item.BusinessId == business.Id &&
            item.Summary.Contains("Complete: True", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UpdateBusinessProfileAsync_WhenDuplicateNameOrEmail_ReturnsSafeError()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var adminApp = provider.GetRequiredService<AdminAppService>();
        var admin = await adminApp.LoginAdminAsync(new AdminLoginCommand(
            "DCAdmin",
            "admin123"));
        var created = await adminApp.CreateBusinessAsync(new CreateBusinessCommand(
            "Duplicate Check Cafe",
            "dupcheck@example.test",
            "startpass1",
            admin!.Id,
            EnablePilot: false,
            Notes: null));

        var duplicateName = await adminApp.UpdateBusinessProfileAsync(new UpdateBusinessProfileCommand(
            created.Business!.BusinessId,
            admin.Id,
            "Demo Coffee",
            "dupcheck@example.test",
            "/img/demo-coffee.svg",
            IsPilotEnabled: false,
            Notes: null));
        var duplicateEmail = await adminApp.UpdateBusinessProfileAsync(new UpdateBusinessProfileCommand(
            created.Business.BusinessId,
            admin.Id,
            "Duplicate Check Cafe",
            "demo@digitalcards.test",
            "/img/demo-coffee.svg",
            IsPilotEnabled: false,
            Notes: null));

        Assert.False(duplicateName.Succeeded);
        Assert.Equal("Ya existe un negocio con ese nombre.", duplicateName.ErrorMessage);
        Assert.False(duplicateEmail.Succeeded);
        Assert.Equal("Ya existe un negocio con ese correo.", duplicateEmail.ErrorMessage);
        Assert.DoesNotContain("password", duplicateName.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", duplicateEmail.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateBusinessBrandingAsync_StoresBrandingAndUsesItForEmailWalletAndClientDashboard()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var adminApp = provider.GetRequiredService<AdminAppService>();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var brandingRepository = provider.GetRequiredService<IBusinessBrandingRepository>();
        var outbox = provider.GetRequiredService<IWalletEmailOutbox>();
        var admin = await adminApp.LoginAdminAsync(new AdminLoginCommand(
            "DCAdmin",
            "admin123"));
        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));

        var result = await adminApp.UpdateBusinessBrandingAsync(new UpdateBusinessBrandingCommand(
            business!.Id,
            admin!.Id,
            "Puntelio Cafe",
            "/img/puntelio.svg",
            "#123456",
            "#abcdef",
            "#fedcba",
            12,
            "Puntelio Rewards",
            "Sellos digitales de Puntelio."));

        Assert.True(result.Succeeded);
        Assert.Equal("Puntelio Cafe", result.Business!.Branding.PublicName);
        Assert.Equal("#123456", result.Business.Branding.PrimaryColor);
        Assert.Equal("#fedcba", result.Business.Branding.CustomFieldColor);
        Assert.Equal(12, result.Business.Branding.StampGoal);

        var stored = await brandingRepository.FindByBusinessIdAsync(business.Id);
        Assert.NotNull(stored);
        Assert.Equal("Puntelio Rewards", stored!.ProgramName);

        var client = await app.RegisterClientAsync(new RegisterClientCommand(
            "branduser",
            "Brand",
            "User",
            "branduser@example.test",
            "ClientPass123!"));
        var enrollment = await app.EnrollClientAsync(new EnrollClientCommand(
            business.Id,
            client.UserName,
            "https://app.puntelio.com"));
        var publicToken = ExtractWalletToken(enrollment.EnrollmentUrl);

        Assert.Equal("Puntelio Cafe", enrollment.Card.BusinessName);
        var message = Assert.Single(await outbox.ListAsync());
        Assert.Equal("Puntelio Cafe", message.BusinessName);
        Assert.Equal("https://app.puntelio.com/img/puntelio.svg", message.BusinessLogoUrl);
        Assert.Equal("#123456", message.PrimaryColor);
        Assert.Equal("Puntelio Rewards", message.ProgramName);

        var landing = await app.GetWalletLandingAsync(publicToken);
        Assert.NotNull(landing);
        Assert.Equal("Puntelio Cafe", landing!.BusinessName);
        Assert.Equal("/img/puntelio.svg", landing.LogoPath);
        Assert.Equal("#123456", landing.PrimaryColor);
        Assert.Equal("#abcdef", landing.SecondaryColor);
        Assert.Equal(12, landing.StampGoal);

        var dashboard = await app.GetClientDashboardAsync(client.Id);
        var card = Assert.Single(dashboard.Cards);
        Assert.Equal("Puntelio Cafe", card.BusinessName);
    }

    [Fact]
    public async Task UpdateBusinessBrandingAsync_WhenBusinessSelfService_StoresBrandingAndFeedsWalletLanding()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));

        Assert.NotNull(business);

        var result = await app.UpdateBusinessBrandingAsync(new UpdateBusinessSelfServiceBrandingCommand(
            business!.Id,
            "Self Service Cafe",
            "/uploads/business-logos/demo/logo.png",
            "#112233",
            "#445566",
            "#778899",
            8,
            "Self Rewards",
            "Sellos desde autoservicio."));

        Assert.True(result.Succeeded);
        Assert.Equal("Self Service Cafe", result.Settings!.Branding.PublicName);
        Assert.Equal("#112233", result.Settings.Branding.PrimaryColor);
        Assert.Equal("#778899", result.Settings.Branding.CustomFieldColor);
        Assert.Equal(8, result.Settings.Branding.StampGoal);

        var client = await app.RegisterClientAsync(new RegisterClientCommand(
            "selfbrand",
            "Self",
            "Brand",
            "selfbrand@example.test",
            "ClientPass123!"));
        var enrollment = await app.EnrollClientAsync(new EnrollClientCommand(
            business.Id,
            client.UserName,
            "https://app.puntelio.com"));
        var landing = await app.GetWalletLandingAsync(ExtractWalletToken(enrollment.EnrollmentUrl));

        Assert.NotNull(landing);
        Assert.Equal("Self Service Cafe", landing!.BusinessName);
        Assert.Equal("/uploads/business-logos/demo/logo.png", landing.LogoPath);
        Assert.Equal("#112233", landing.PrimaryColor);
        Assert.Equal("#445566", landing.SecondaryColor);
        Assert.Equal(8, landing.StampGoal);
    }

    [Fact]
    public async Task AddStampAsync_UsesBusinessStampGoal()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));
        Assert.NotNull(business);

        await app.UpdateBusinessBrandingAsync(new UpdateBusinessSelfServiceBrandingCommand(
            business!.Id,
            "Goal Cafe",
            "/img/demo-coffee.svg",
            "#112233",
            "#445566",
            "#ffffff",
            2,
            "Goal Rewards",
            "Recompensa al completar dos sellos."));

        var client = await app.RegisterClientAsync(new RegisterClientCommand(
            "goaluser",
            "Goal",
            "User",
            "goaluser@example.test",
            "ClientPass123!"));
        await app.EnrollClientAsync(new EnrollClientCommand(
            business.Id,
            client.UserName,
            "https://app.puntelio.com"));

        var secondStamp = await app.AddStampAsync(new AddStampCommand(business.Id, client.UserName));
        var completeException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            app.AddStampAsync(new AddStampCommand(business.Id, client.UserName)));
        var detail = await app.GetBusinessCardForClientAsync(business.Id, client.UserName);
        Assert.NotNull(detail);
        var redemption = await app.RedeemRewardAsync(business.Id, detail!.Id);

        Assert.Equal(2, secondStamp.CurrentStamps);
        Assert.Equal(2, secondStamp.StampGoal);
        Assert.Equal(2, secondStamp.VisibleStamps);
        Assert.True(secondStamp.RewardReady);
        Assert.Equal(0, secondStamp.StampsRemaining);
        Assert.Contains("canje", completeException.Message);
        Assert.True(redemption.Succeeded);
        Assert.Equal(0, redemption.Card!.CurrentStamps);
        Assert.Equal(2, redemption.Card.StampGoal);
        Assert.Equal(0, redemption.Card.VisibleStamps);
        Assert.False(redemption.Card.RewardReady);
        Assert.Equal(2, redemption.Card.StampsRemaining);
        Assert.Equal(2, redemption.Redemption!.RedeemedCheckQTY);
        Assert.Equal(2, redemption.Redemption.StampGoal);
        Assert.Contains("dos sellos", redemption.Redemption.RewardText);
        Assert.Contains(redemption.Card.RecentStampEvents, item => item.Source == StampLedgerSource.RewardRedeemed);
        Assert.Single(redemption.Card.RecentRewardRedemptions);
    }

    [Fact]
    public async Task RedeemRewardAsync_WithIncompleteCard_DoesNotMutateStamps()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));
        Assert.NotNull(business);

        var client = await app.RegisterClientAsync(new RegisterClientCommand(
            "redeemnine",
            "Redeem",
            "Nine",
            "redeemnine@example.test",
            "ClientPass123!"));
        await app.EnrollClientAsync(new EnrollClientCommand(
            business!.Id,
            client.UserName,
            "https://app.puntelio.com"));
        for (var index = 0; index < 8; index++)
        {
            await app.AddStampAsync(new AddStampCommand(business.Id, client.UserName));
        }

        var detail = await app.GetBusinessCardForClientAsync(business.Id, client.UserName);
        Assert.NotNull(detail);
        Assert.Equal(9, detail!.CurrentStamps);
        Assert.False(detail.RewardReady);

        var redemption = await app.RedeemRewardAsync(business.Id, detail.Id);
        var persisted = await app.GetBusinessCardForClientAsync(business.Id, client.UserName);

        Assert.False(redemption.Succeeded);
        Assert.Contains("aun no esta completa", redemption.ErrorMessage);
        Assert.Equal(9, redemption.Card!.CurrentStamps);
        Assert.False(redemption.Card.RewardReady);
        Assert.Equal(9, persisted!.CurrentStamps);
        Assert.False(persisted.RewardReady);
    }

    [Fact]
    public async Task RedeemRewardAsync_NotifiesWalletsWithResetCard()
    {
        var services = CreateDefaultServices();
        var google = new RecordingGoogleWalletService();
        var apple = new RecordingAppleWalletService();
        foreach (var descriptor in services.Where(descriptor => descriptor.ServiceType == typeof(IGoogleWalletService)).ToArray())
        {
            services.Remove(descriptor);
        }

        foreach (var descriptor in services.Where(descriptor => descriptor.ServiceType == typeof(IAppleWalletService)).ToArray())
        {
            services.Remove(descriptor);
        }

        services.AddSingleton<IGoogleWalletService>(google);
        services.AddSingleton<IAppleWalletService>(apple);
        var provider = services.BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));
        Assert.NotNull(business);

        await app.UpdateBusinessBrandingAsync(new UpdateBusinessSelfServiceBrandingCommand(
            business!.Id,
            "Goal Cafe",
            "/img/demo-coffee.svg",
            "#112233",
            "#445566",
            "#ffffff",
            2,
            "Goal Rewards",
            "Recompensa al completar dos sellos."));
        var client = await app.RegisterClientAsync(new RegisterClientCommand(
            "redeemwallet",
            "Redeem",
            "Wallet",
            "redeemwallet@example.test",
            "ClientPass123!"));
        var enrollment = await app.EnrollClientAsync(new EnrollClientCommand(
            business.Id,
            client.UserName,
            "https://app.puntelio.com"));
        await app.SelectGoogleWalletAsync(ExtractWalletToken(enrollment.EnrollmentUrl));
        await app.AddStampAsync(new AddStampCommand(business.Id, client.UserName));

        var detail = await app.GetBusinessCardForClientAsync(business.Id, client.UserName);
        Assert.NotNull(detail);
        Assert.True(detail!.RewardReady);

        var redemption = await app.RedeemRewardAsync(business.Id, detail.Id);

        Assert.True(redemption.Succeeded);
        Assert.Equal(0, redemption.Card!.CurrentStamps);
        Assert.Equal(0, google.PatchedCurrentStamps);
        Assert.Equal(0, apple.NotifiedCurrentStamps);
    }

    [Fact]
    public async Task RedeemRewardAsync_WhenRewardTableIsUnavailable_DoesNotResetCompleteCard()
    {
        var services = CreateDefaultServices();
        foreach (var descriptor in services.Where(descriptor => descriptor.ServiceType == typeof(IRewardRedemptionRepository)).ToArray())
        {
            services.Remove(descriptor);
        }

        services.AddSingleton<IRewardRedemptionRepository, UnavailableRewardRedemptionRepository>();
        var provider = services.BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));
        Assert.NotNull(business);

        await app.UpdateBusinessBrandingAsync(new UpdateBusinessSelfServiceBrandingCommand(
            business!.Id,
            "Goal Cafe",
            "/img/demo-coffee.svg",
            "#112233",
            "#445566",
            "#ffffff",
            2,
            "Goal Rewards",
            "Recompensa al completar dos sellos."));
        var client = await app.RegisterClientAsync(new RegisterClientCommand(
            "missingreward",
            "Missing",
            "Reward",
            "missingreward@example.test",
            "ClientPass123!"));
        await app.EnrollClientAsync(new EnrollClientCommand(
            business.Id,
            client.UserName,
            "https://app.puntelio.com"));
        await app.AddStampAsync(new AddStampCommand(business.Id, client.UserName));

        var detail = await app.GetBusinessCardForClientAsync(business.Id, client.UserName);
        Assert.NotNull(detail);
        Assert.True(detail!.RewardReady);

        var redemption = await app.RedeemRewardAsync(business.Id, detail.Id);
        var persisted = await app.GetBusinessCardForClientAsync(business.Id, client.UserName);

        Assert.False(redemption.Succeeded);
        Assert.Equal("No se puede canjear la recompensa en este momento. Contacta a soporte.", redemption.ErrorMessage);
        Assert.DoesNotContain(".sql", redemption.ErrorMessage);
        Assert.DoesNotContain("migration-context", redemption.ErrorMessage);
        Assert.DoesNotContain("RewardRedemption", redemption.ErrorMessage);
        Assert.Equal(2, redemption.Card!.CurrentStamps);
        Assert.True(redemption.Card.RewardReady);
        Assert.Equal(2, persisted!.CurrentStamps);
        Assert.True(persisted.RewardReady);
        Assert.Empty(persisted.RecentRewardRedemptions);
    }

    [Fact]
    public async Task SelectGoogleWalletAsync_PatchesExistingObjectWithCurrentBranding()
    {
        var services = CreateDefaultServices();
        var google = new RecordingGoogleWalletService();
        foreach (var descriptor in services.Where(descriptor => descriptor.ServiceType == typeof(IGoogleWalletService)).ToArray())
        {
            services.Remove(descriptor);
        }

        services.AddSingleton<IGoogleWalletService>(google);
        var provider = services.BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));
        Assert.NotNull(business);

        var client = await app.RegisterClientAsync(new RegisterClientCommand(
            "runnigoogle",
            "Runni",
            "User",
            "runnigoogle@example.test",
            "ClientPass123!"));
        var enrollment = await app.EnrollClientAsync(new EnrollClientCommand(
            business!.Id,
            client.UserName,
            "https://app.puntelio.com"));
        var token = ExtractWalletToken(enrollment.EnrollmentUrl);

        await app.SelectGoogleWalletAsync(token);
        await app.UpdateBusinessBrandingAsync(new UpdateBusinessSelfServiceBrandingCommand(
            business.Id,
            "Runni Cafe",
            "/img/runni.png",
            "#112233",
            "#445566",
            "#ffffff",
            10,
            "Runni Rewards",
            "Cafe gratis."));
        await app.SelectGoogleWalletAsync(token);

        Assert.Equal("Runni Cafe", google.PatchedBusinessName);
        Assert.Equal("Runni Rewards", google.PatchedProgramName);
        Assert.Equal("#112233", google.PatchedPrimaryColor);
    }

    [Fact]
    public async Task ResetBusinessPasswordAsync_UpdatesLegacyAndModernCredentials()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var adminApp = provider.GetRequiredService<AdminAppService>();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var businesses = provider.GetRequiredService<IBusinessRepository>();
        var credentials = provider.GetRequiredService<IBusinessCredentialRepository>();
        var admin = await adminApp.LoginAdminAsync(new AdminLoginCommand(
            "DCAdmin",
            "admin123"));
        const string oldPassword = "startpass1";
        const string newPassword = "newpass123";
        var created = await adminApp.CreateBusinessAsync(new CreateBusinessCommand(
            "Reset Cafe",
            "reset@example.test",
            oldPassword,
            admin!.Id,
            EnablePilot: true,
            Notes: null));

        var result = await adminApp.ResetBusinessPasswordAsync(new ResetBusinessPasswordCommand(
            created.Business!.BusinessId,
            admin.Id,
            newPassword));

        Assert.True(result.Succeeded);
        var business = await businesses.FindByIdAsync(created.Business.BusinessId);
        Assert.NotNull(business);
        Assert.Equal(ExpectedLegacyBusinessPasswordHash(newPassword), business!.PasswordHashPlaceholder);
        Assert.DoesNotContain(newPassword, business.PasswordHashPlaceholder, StringComparison.Ordinal);
        var credential = await credentials.FindByBusinessIdAsync(business.Id);
        Assert.NotNull(credential);
        Assert.DoesNotContain(newPassword, credential!.PasswordHash, StringComparison.Ordinal);

        Assert.Null(await app.LoginBusinessAsync(new BusinessLoginCommand(
            "reset@example.test",
            oldPassword)));
        Assert.NotNull(await app.LoginBusinessAsync(new BusinessLoginCommand(
            "reset@example.test",
            newPassword)));
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
    public async Task RegisterClientAsync_CreatesLegacyAndModernClientCredential()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var clients = provider.GetRequiredService<IClientRepository>();
        var credentials = provider.GetRequiredService<IClientCredentialRepository>();
        const string password = "clientpass1";

        var registered = await app.RegisterClientAsync(new RegisterClientCommand(
            "clientlogin1",
            "Client",
            "Login",
            "clientlogin1@example.test",
            password));

        var client = await clients.FindByIdAsync(registered.Id);
        Assert.NotNull(client);
        Assert.Equal(ExpectedLegacyBusinessPasswordHash(password), client!.PasswordHashPlaceholder);
        Assert.DoesNotContain(password, client.PasswordHashPlaceholder, StringComparison.Ordinal);

        var credential = await credentials.FindByClientIdAsync(registered.Id);
        Assert.NotNull(credential);
        Assert.True(credential!.PasswordHash.Length > 25);
        Assert.DoesNotContain(password, credential.PasswordHash, StringComparison.Ordinal);

        var login = await app.LoginClientAsync(new ClientLoginCommand(
            "clientlogin1",
            password));

        Assert.NotNull(login);
        Assert.Equal(registered.Id, login!.Id);
        Assert.Equal("clientlogin1@example.test", login.Email);
    }

    [Fact]
    public async Task RegisterClientAsync_NormalizesUserNameAndRejectsInvalidCharacters()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();

        var registered = await app.RegisterClientAsync(new RegisterClientCommand(
            "ClientABC123",
            "Client",
            "Normalized",
            "clientabc123@example.test",
            "clientpass1"));
        var invalid = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            app.RegisterClientAsync(new RegisterClientCommand(
                "bad user",
                "Bad",
                "User",
                "baduser@example.test",
                "clientpass1")));

        Assert.Equal("clientabc123", registered.UserName);
        Assert.Equal("El usuario solo puede usar letras y numeros, sin espacios.", invalid.Message);
        Assert.NotNull(await app.LoginClientAsync(new ClientLoginCommand("CLIENTABC123", "clientpass1")));
    }

    [Fact]
    public async Task RecordClientConsentAsync_StoresConsentWithoutSecrets()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var consentRepository = provider.GetRequiredService<IClientConsentRepository>();
        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));
        var client = await app.RegisterClientAsync(new RegisterClientCommand(
            "consent1",
            "Consent",
            "Client",
            "consent1@example.test",
            "clientpass1"));

        await app.RecordClientConsentAsync(
            new RecordClientConsentCommand(
                client.Id,
                business!.Id,
                "privacy-2026-05",
                "PublicBusinessEnrollment"));

        var consents = await consentRepository.ListByClientIdAsync(client.Id);
        var consent = Assert.Single(consents);
        Assert.Equal(business.Id, consent.BusinessId);
        Assert.Equal("privacy-2026-05", consent.PolicyVersion);
        Assert.Equal("PublicBusinessEnrollment", consent.Source);
        Assert.DoesNotContain("clientpass1", consent.Source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginClient_WithLegacyPassword_CreatesModernCredential()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var clients = provider.GetRequiredService<IClientRepository>();
        var credentials = provider.GetRequiredService<IClientCredentialRepository>();
        const string password = "clientpass1";
        var client = new Client(
            Guid.NewGuid(),
            "legacyclient1",
            "Legacy",
            "Client",
            "legacyclient1@example.test",
            ExpectedLegacyBusinessPasswordHash(password));
        await clients.AddAsync(client);

        var login = await app.LoginClientAsync(new ClientLoginCommand(
            "legacyclient1",
            password));

        Assert.NotNull(login);
        var credential = await credentials.FindByClientIdAsync(client.Id);
        Assert.NotNull(credential);
        Assert.DoesNotContain(password, credential!.PasswordHash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoginClient_UsesModernCredentialAfterLegacyPasswordChanges()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var clients = provider.GetRequiredService<IClientRepository>();
        const string password = "clientpass1";
        var registered = await app.RegisterClientAsync(new RegisterClientCommand(
            "modernclient1",
            "Modern",
            "Client",
            "modernclient1@example.test",
            password));

        await clients.UpdatePasswordAsync(registered.Id, "legacy-password-changed");

        var login = await app.LoginClientAsync(new ClientLoginCommand(
            "modernclient1",
            password));

        Assert.NotNull(login);
        Assert.Equal(registered.Id, login!.Id);
    }

    [Fact]
    public async Task LoginClient_WithInvalidOrBusinessCredentials_ReturnsNull()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var clients = provider.GetRequiredService<IClientRepository>();
        var credentials = provider.GetRequiredService<IClientCredentialRepository>();
        var client = new Client(
            Guid.NewGuid(),
            "clientlogin2",
            "Client",
            "Login",
            "clientlogin2@example.test",
            ExpectedLegacyBusinessPasswordHash("clientpass1"));
        await clients.AddAsync(client);

        var invalidPassword = await app.LoginClientAsync(new ClientLoginCommand(
            "clientlogin2",
            "wrong-password"));
        var businessCredentials = await app.LoginClientAsync(new ClientLoginCommand(
            "demo@digitalcards.test",
            "business123"));

        Assert.Null(invalidPassword);
        Assert.Null(businessCredentials);
        Assert.Null(await credentials.FindByClientIdAsync(client.Id));
    }

    [Fact]
    public async Task ChangeClientPasswordAsync_UpdatesLegacyAndModernCredentials()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var clients = provider.GetRequiredService<IClientRepository>();
        var credentials = provider.GetRequiredService<IClientCredentialRepository>();
        const string oldPassword = "clientpass1";
        const string newPassword = "changedpass1";
        var registered = await app.RegisterClientAsync(new RegisterClientCommand(
            "changepass1",
            "Change",
            "Client",
            "changepass1@example.test",
            oldPassword));

        var result = await app.ChangeClientPasswordAsync(new ChangeClientPasswordCommand(
            registered.Id,
            oldPassword,
            newPassword));

        Assert.True(result.Succeeded);
        var client = await clients.FindByIdAsync(registered.Id);
        Assert.NotNull(client);
        Assert.Equal(ExpectedLegacyBusinessPasswordHash(newPassword), client!.PasswordHashPlaceholder);
        Assert.DoesNotContain(newPassword, client.PasswordHashPlaceholder, StringComparison.Ordinal);
        var credential = await credentials.FindByClientIdAsync(registered.Id);
        Assert.NotNull(credential);
        Assert.DoesNotContain(newPassword, credential!.PasswordHash, StringComparison.Ordinal);

        Assert.Null(await app.LoginClientAsync(new ClientLoginCommand("changepass1", oldPassword)));
        Assert.NotNull(await app.LoginClientAsync(new ClientLoginCommand("changepass1", newPassword)));
    }

    [Fact]
    public async Task ChangeClientPasswordAsync_WithWrongCurrentPassword_DoesNotUpdateCredential()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var credentials = provider.GetRequiredService<IClientCredentialRepository>();
        const string oldPassword = "clientpass1";
        var registered = await app.RegisterClientAsync(new RegisterClientCommand(
            "changepass2",
            "Change",
            "Client",
            "changepass2@example.test",
            oldPassword));
        var before = await credentials.FindByClientIdAsync(registered.Id);

        var result = await app.ChangeClientPasswordAsync(new ChangeClientPasswordCommand(
            registered.Id,
            "wrong-password",
            "changedpass1"));

        Assert.False(result.Succeeded);
        Assert.Equal("La contrasena actual no es valida.", result.ErrorMessage);
        var after = await credentials.FindByClientIdAsync(registered.Id);
        Assert.Equal(before!.PasswordHash, after!.PasswordHash);
    }

    [Fact]
    public async Task UpdateClientProfileAsync_UpdatesLegacyProfileAndKeepsUserNameStable()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var registered = await app.RegisterClientAsync(new RegisterClientCommand(
            "profileclient1",
            "Profile",
            "Client",
            "profileclient1@example.test",
            "clientpass1"));

        var result = await app.UpdateClientProfileAsync(new UpdateClientProfileCommand(
            registered.Id,
            "Updated",
            "Person",
            "profile1new@example.test"));

        Assert.True(result.Succeeded);
        Assert.Equal("profileclient1", result.Client!.UserName);
        Assert.Equal("Updated", result.Client.FirstName);
        Assert.Equal("Person", result.Client.LastName);
        Assert.Equal("profile1new@example.test", result.Client.Email);
        var profile = await app.GetClientProfileAsync(registered.Id);
        Assert.Equal("profileclient1", profile!.UserName);
        Assert.Equal("Updated", profile.FirstName);
        Assert.Equal("profile1new@example.test", profile.Email);
    }

    [Fact]
    public async Task UpdateClientProfileAsync_DuplicateEmailFailsSafely()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var first = await app.RegisterClientAsync(new RegisterClientCommand(
            "profileclient2",
            "Profile",
            "Client",
            "profileclient2@example.test",
            "clientpass1"));
        await app.RegisterClientAsync(new RegisterClientCommand(
            "profileclient3",
            "Profile",
            "Other",
            "profileclient3@example.test",
            "clientpass1"));

        var result = await app.UpdateClientProfileAsync(new UpdateClientProfileCommand(
            first.Id,
            "Profile",
            "Client",
            "profileclient3@example.test"));

        Assert.False(result.Succeeded);
        Assert.Equal("El correo ya esta registrado.", result.ErrorMessage);
        var profile = await app.GetClientProfileAsync(first.Id);
        Assert.Equal("profileclient2@example.test", profile!.Email);
    }

    [Fact]
    public async Task GetClientDashboardAsync_ReturnsProfileWalletSummaryAndOpaqueLinks()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var applePasses = provider.GetRequiredService<IAppleWalletPassRepository>();
        var client = await app.RegisterClientAsync(new RegisterClientCommand(
            "dashclient1",
            "Dash",
            "Client",
            "dashclient1@example.test",
            "clientpass1"));
        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));
        var enrollment = await app.EnrollClientAsync(new EnrollClientCommand(
            business!.Id,
            client.UserName,
            "http://localhost"));
        var token = ExtractWalletToken(enrollment.EnrollmentUrl);
        await app.SelectGoogleWalletAsync(token);
        var now = DateTimeOffset.UtcNow;
        await applePasses.UpsertPassAsync(new AppleWalletPassRecord(
            "pass.test",
            "serial-1",
            enrollment.Card.Id,
            "auth-hash",
            "1",
            now,
            now));
        await applePasses.UpsertDeviceAsync(new AppleWalletDeviceRecord(
            "device-1",
            "push-token",
            now,
            now));
        await applePasses.AddRegistrationAsync("device-1", "pass.test", "serial-1", now);

        var dashboard = await app.GetClientDashboardAsync(client.Id);

        Assert.Equal(client.Id, dashboard.Client.Id);
        Assert.Equal("dashclient1@example.test", dashboard.Client.Email);
        Assert.Equal(1, dashboard.TotalCurrentStamps);
        Assert.Equal(1, dashboard.TotalLifetimeStamps);
        Assert.Equal(1, dashboard.GoogleIssuedCount);
        Assert.Equal(1, dashboard.AppleTrackedCount);
        var card = Assert.Single(dashboard.Cards);
        Assert.Equal("Demo Coffee", card.BusinessName);
        Assert.True(card.GoogleIssued);
        Assert.True(card.AppleTracked);
        Assert.Equal(1, card.AppleRegisteredDeviceCount);
        Assert.NotEqual(enrollment.Card.EnrollmentToken, card.WalletSelectToken);
        Assert.DoesNotContain(enrollment.Card.EnrollmentToken, card.WalletSelectToken, StringComparison.OrdinalIgnoreCase);
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
    public void EmailTemplateRenderer_WalletTemplateUsesBrandingAndEscapesHtml()
    {
        var renderer = new EmailTemplateRenderer();

        var rendered = renderer.RenderWalletEnrollment(new WalletEnrollmentEmail(
            "maria@example.test",
            "Maria <script>",
            "Puntelio <Cafe>",
            "https://app.puntelio.com/Wallet/Select/token-123",
            DateTimeOffset.UtcNow,
            "https://app.puntelio.com/img/logo.png",
            "#123456",
            "Puntelio Rewards"));

        Assert.Equal(EmailTemplateKind.WalletEnrollment, rendered.Kind);
        Assert.Equal("Tu tarjeta digital de Puntelio <Cafe> esta lista", rendered.Subject);
        Assert.Contains("Puntelio Rewards", rendered.HtmlBody);
        Assert.Contains("#123456", rendered.HtmlBody);
        Assert.Contains("https://app.puntelio.com/img/logo.png", rendered.HtmlBody);
        Assert.Contains("https://app.puntelio.com/img/add_to_apple_wallet.svg", rendered.HtmlBody);
        Assert.Contains("https://app.puntelio.com/img/add_to_google_wallet.svg", rendered.HtmlBody);
        Assert.Contains("https://app.puntelio.com/Wallet/Apple/token-123", rendered.HtmlBody);
        Assert.Contains("https://app.puntelio.com/Wallet/Google/token-123", rendered.HtmlBody);
        Assert.Contains("Maria &lt;script&gt;", rendered.HtmlBody);
        Assert.Contains("Puntelio &lt;Cafe&gt;", rendered.HtmlBody);
        Assert.DoesNotContain("<script>", rendered.HtmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("https://app.puntelio.com/Wallet/Select/token-123", rendered.TextBody);
    }

    [Fact]
    public void EmailTemplateRenderer_RendersWelcomeResetAndInternalAlertTemplates()
    {
        var renderer = new EmailTemplateRenderer();
        var brand = new EmailBranding(
            "Puntelio",
            "javascript:alert(1)",
            "not-a-color",
            "Puntelio Rewards");

        var welcome = renderer.RenderWelcome(new WelcomeEmail(
            "client@example.test",
            "Cliente Uno",
            "https://app.puntelio.com/Client/Login",
            DateTimeOffset.UtcNow,
            brand));
        var reset = renderer.RenderPasswordReset(new PasswordResetEmail(
            "client@example.test",
            "Cliente Uno",
            "cliente",
            "https://app.puntelio.com/Client/ResetPassword/token",
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow,
            brand));
        var alert = renderer.RenderInternalAlert(new InternalAlertEmail(
            "ops@example.test",
            "Wallet update failed",
            "Google patch returned a retryable status.",
            "Warning",
            "https://app.puntelio.com/internal/wallet-diagnostics/1",
            DateTimeOffset.UtcNow));

        Assert.Equal(EmailTemplateKind.Welcome, welcome.Kind);
        Assert.Contains("Bienvenido a Puntelio", welcome.Subject);
        Assert.Contains("https://app.puntelio.com/Client/Login", welcome.HtmlBody);
        Assert.DoesNotContain("javascript:", welcome.HtmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("#111827", welcome.HtmlBody);

        Assert.Equal(EmailTemplateKind.PasswordReset, reset.Kind);
        Assert.Contains("Restablece tu contrasena", reset.Subject);
        Assert.Contains("https://app.puntelio.com/Client/ResetPassword/token", reset.HtmlBody);
        Assert.DoesNotContain("hash", reset.HtmlBody, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(EmailTemplateKind.InternalAlert, alert.Kind);
        Assert.Contains("[Warning] Wallet update failed", alert.Subject);
        Assert.Contains("Google patch returned a retryable status.", alert.TextBody);
        Assert.Contains("https://app.puntelio.com/internal/wallet-diagnostics/1", alert.HtmlBody);
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
            "mariatest",
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
    public async Task ClientPasswordReset_UsesHashedOneTimeTokenAndUpdatesCredentials()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var outbox = provider.GetRequiredService<IPasswordResetEmailOutbox>();
        var store = provider.GetRequiredService<InMemoryDigitalCardsStore>();
        const string oldPassword = "OldClient123!";
        const string newPassword = "NewClient123!";
        var client = await app.RegisterClientAsync(new RegisterClientCommand(
            "resetclient1",
            "Reset",
            "Client",
            "resetclient1@example.test",
            oldPassword));

        var request = await app.RequestClientPasswordResetAsync(
            new RequestClientPasswordResetCommand("resetclient1", "https://app.puntelio.com"));

        Assert.True(request.Accepted);
        var message = Assert.Single(await outbox.ListPasswordResetsAsync());
        Assert.Equal("resetclient1@example.test", message.To);
        Assert.Contains("/Client/ResetPassword/", message.ResetUrl);
        var plainToken = ExtractResetToken(message.ResetUrl, "/Client/ResetPassword/");
        var tokenRecord = Assert.Single(store.PasswordResetTokens);
        Assert.Equal(PasswordResetAccountType.Client, tokenRecord.AccountType);
        Assert.Equal(client.Id, tokenRecord.AccountId);
        Assert.Equal(64, tokenRecord.TokenHash.Length);
        Assert.DoesNotContain(plainToken, tokenRecord.TokenHash, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(plainToken[^8..], tokenRecord.TokenSuffix);

        var reset = await app.ResetClientPasswordAsync(new ResetPasswordCommand(plainToken, newPassword));
        var replay = await app.ResetClientPasswordAsync(new ResetPasswordCommand(plainToken, "AnotherClient123!"));

        Assert.True(reset.Succeeded);
        Assert.False(replay.Succeeded);
        Assert.Null(await app.LoginClientAsync(new ClientLoginCommand("resetclient1", oldPassword)));
        Assert.NotNull(await app.LoginClientAsync(new ClientLoginCommand("resetclient1", newPassword)));
        var credential = await provider.GetRequiredService<IClientCredentialRepository>()
            .FindByClientIdAsync(client.Id);
        Assert.NotNull(credential);
        Assert.DoesNotContain(newPassword, credential!.PasswordHash, StringComparison.Ordinal);
        Assert.NotNull(store.PasswordResetTokens.Single().UsedAt);
    }

    [Fact]
    public async Task ClientPasswordReset_RequestForMissingAccountDoesNotSendEmail()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var outbox = provider.GetRequiredService<IPasswordResetEmailOutbox>();

        var request = await app.RequestClientPasswordResetAsync(
            new RequestClientPasswordResetCommand("missing@example.test", "https://app.puntelio.com"));

        Assert.True(request.Accepted);
        Assert.Empty(await outbox.ListPasswordResetsAsync());
    }

    [Fact]
    public async Task BusinessPasswordReset_UpdatesLegacyAndModernCredentials()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var outbox = provider.GetRequiredService<IPasswordResetEmailOutbox>();
        var store = provider.GetRequiredService<InMemoryDigitalCardsStore>();
        const string oldPassword = "business123";
        const string newPassword = "NewBusiness123!";

        var request = await app.RequestBusinessPasswordResetAsync(
            new RequestBusinessPasswordResetCommand("demo@digitalcards.test", "https://app.puntelio.com"));

        Assert.True(request.Accepted);
        var message = Assert.Single(await outbox.ListPasswordResetsAsync());
        Assert.Equal("demo@digitalcards.test", message.To);
        Assert.Contains("/Business/ResetPassword/", message.ResetUrl);
        var plainToken = ExtractResetToken(message.ResetUrl, "/Business/ResetPassword/");
        var tokenRecord = Assert.Single(store.PasswordResetTokens);
        Assert.Equal(PasswordResetAccountType.Business, tokenRecord.AccountType);
        Assert.DoesNotContain(plainToken, tokenRecord.TokenHash, StringComparison.OrdinalIgnoreCase);

        var reset = await app.ResetBusinessPasswordAsync(new ResetPasswordCommand(plainToken, newPassword));

        Assert.True(reset.Succeeded);
        Assert.Null(await app.LoginBusinessAsync(new BusinessLoginCommand("demo@digitalcards.test", oldPassword)));
        Assert.NotNull(await app.LoginBusinessAsync(new BusinessLoginCommand("demo@digitalcards.test", newPassword)));
        var business = await provider.GetRequiredService<IBusinessRepository>()
            .FindByEmailAsync("demo@digitalcards.test");
        Assert.NotNull(business);
        Assert.Equal(ExpectedLegacyBusinessPasswordHash(newPassword), business!.PasswordHashPlaceholder);
        var credential = await provider.GetRequiredService<IBusinessCredentialRepository>()
            .FindByBusinessIdAsync(business.Id);
        Assert.NotNull(credential);
        Assert.DoesNotContain(newPassword, credential!.PasswordHash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PasswordReset_WithInvalidOrExpiredTokenFailsSafely()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var store = provider.GetRequiredService<InMemoryDigitalCardsStore>();
        var client = await app.RegisterClientAsync(new RegisterClientCommand(
            "expiredreset",
            "Expired",
            "Reset",
            "expiredreset@example.test",
            "OldClient123!"));
        const string plainToken = "expired-token";
        store.PasswordResetTokens.Add(new PasswordResetTokenRecord(
            99,
            PasswordResetAccountType.Client,
            client.Id,
            Sha256Hex(plainToken),
            "ed-token",
            DateTimeOffset.UtcNow.AddHours(-2),
            DateTimeOffset.UtcNow.AddHours(-1),
            UsedAt: null,
            RevokedAt: null));

        var expired = await app.ResetClientPasswordAsync(new ResetPasswordCommand(plainToken, "NewClient123!"));
        var missing = await app.ResetClientPasswordAsync(new ResetPasswordCommand("missing-token", "NewClient123!"));

        Assert.False(expired.Succeeded);
        Assert.Equal("El link no es valido o ya expiro.", expired.ErrorMessage);
        Assert.False(missing.Succeeded);
        Assert.Equal("El link no es valido o ya expiro.", missing.ErrorMessage);
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
            "appletest",
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
        var hardenedProvider = CreateDefaultServices().BuildServiceProvider();
        var hardenedApp = hardenedProvider.GetRequiredService<DigitalCardsAppService>();
        var hardenedEnrollment = await CreateEnrollmentAsync(hardenedApp, "blockedtoken");
        var publicToken = ExtractWalletToken(hardenedEnrollment.EnrollmentUrl);

        Assert.Null(await hardenedApp.GetWalletLandingAsync(hardenedEnrollment.Card.EnrollmentToken));
        Assert.NotNull(await hardenedApp.GetWalletLandingAsync(publicToken));

        var compatibleServices = CreateDefaultServices(new Dictionary<string, string?>
        {
            ["DigitalCards:WalletLinks:AllowLegacyCardIdTokens"] = "true"
        });
        var compatibleProvider = compatibleServices.BuildServiceProvider();
        var compatibleApp = compatibleProvider.GetRequiredService<DigitalCardsAppService>();
        var compatibleEnrollment = await CreateEnrollmentAsync(compatibleApp, "compattoken");

        var legacyLanding = await compatibleApp.GetWalletLandingAsync(compatibleEnrollment.Card.EnrollmentToken);

        Assert.NotNull(legacyLanding);
    }

    [Fact]
    public async Task AddStampToCardAsync_RecordsModernBusinessStampLedger()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));
        var enrollment = await CreateEnrollmentAsync(app, "ledgerok");

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
        var enrollment = await CreateEnrollmentAsync(app, "ledgerfail");

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
    public async Task GetBusinessDashboardAsync_ReturnsRecentCardsWalletStateAndLedger()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));
        var enrollment = await CreateEnrollmentAsync(app, "bizdash1");
        var token = ExtractWalletToken(enrollment.EnrollmentUrl);
        await app.SelectGoogleWalletAsync(token);
        await app.AddStampToCardAsync(business!.Id, enrollment.Card.Id);

        var dashboard = await app.GetBusinessDashboardAsync(business.Id);

        Assert.NotNull(dashboard);
        Assert.Equal("Demo Coffee", dashboard!.Business.Name);
        Assert.Equal(1, dashboard.RecentCardCount);
        Assert.Equal(2, dashboard.CurrentStampTotal);
        Assert.Equal(2, dashboard.LifetimeStampTotal);
        Assert.Equal(1, dashboard.GoogleIssuedCount);
        var card = Assert.Single(dashboard.RecentCards);
        Assert.Equal("bizdash1", card.Client.UserName);
        Assert.True(card.GoogleIssued);
        var ledger = Assert.Single(dashboard.RecentStampEvents);
        Assert.Equal(card.Id, ledger.CardId);
        Assert.Equal("bizdash1", ledger.ClientUserName);
        Assert.Equal(StampLedgerSource.ModernBusiness, ledger.Source);
        Assert.Equal(1, ledger.PreviousCheckQTY);
        Assert.Equal(2, ledger.NewCheckQTY);
        Assert.True(ledger.GoogleWalletAttempted);
        Assert.True(ledger.GoogleWalletSucceeded);
        Assert.True(ledger.AppleWalletAttempted);
        Assert.True(ledger.AppleWalletSucceeded);
        Assert.Equal(0, dashboard.WalletIssueCount);
    }

    [Fact]
    public async Task GetBusinessDashboardAsync_UsesBrandingPublicName()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));
        Assert.NotNull(business);

        await app.UpdateBusinessBrandingAsync(new UpdateBusinessSelfServiceBrandingCommand(
            business!.Id,
            "Runni Cafe",
            "/img/runni.png",
            "#112233",
            "#445566",
            "#ffffff",
            10,
            "Runni Rewards",
            "Cafe gratis."));

        var dashboard = await app.GetBusinessDashboardAsync(business.Id);
        var shell = await app.GetBusinessShellAsync(business.Id);

        Assert.NotNull(dashboard);
        Assert.Equal("Runni Cafe", dashboard!.Business.Name);
        Assert.NotNull(shell);
        Assert.Equal("Runni Cafe", shell!.Name);
    }

    [Fact]
    public async Task GetBusinessReportsAsync_ReturnsBusinessScopedOperationalReports()
    {
        var provider = CreateDefaultServices().BuildServiceProvider();
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));
        var enrollment = await CreateEnrollmentAsync(app, "bizreport1");
        var token = ExtractWalletToken(enrollment.EnrollmentUrl);
        await app.SelectGoogleWalletAsync(token);
        await app.AddStampToCardAsync(business!.Id, enrollment.Card.Id);

        var reports = await app.GetBusinessReportsAsync(business.Id);

        Assert.NotNull(reports);
        Assert.Equal("Demo Coffee", reports!.Business.Name);
        Assert.Equal(1, reports.CardCount);
        Assert.Equal(1, reports.CardsCreatedLast30Days);
        Assert.Equal(1, reports.ClientCount);
        Assert.Equal(2, reports.CurrentStampTotal);
        Assert.Equal(2, reports.LifetimeStampTotal);
        Assert.Equal(1, reports.StampsLast30Days);
        Assert.Equal(1, reports.GoogleIssuedCount);
        Assert.Equal(0, reports.GooglePendingCount);
        Assert.True(reports.StampPeriods.Count >= 1);
        var client = Assert.Single(reports.RecentClients);
        Assert.Equal("bizreport1", client.UserName);
        Assert.Equal("Token User", client.ClientName);
        Assert.Equal(1, client.CardCount);
        Assert.Equal(2, client.CurrentStamps);
        Assert.Equal(2, client.LifetimeStamps);
        Assert.Equal("Lista", client.CardStatus);
        Assert.Equal(0, reports.WalletIssueCount);
        Assert.Empty(reports.RecentWalletIssues);
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

    private static string ExtractResetToken(string resetUrl, string marker)
    {
        var index = resetUrl.IndexOf(marker, StringComparison.Ordinal);
        return index < 0
            ? throw new InvalidOperationException("Password reset link was not found.")
            : resetUrl[(index + marker.Length)..];
    }

    private static string ExpectedLegacyBusinessPasswordHash(string password)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password))).ToLowerInvariant();
        return hash[..25];
    }

    private static string Sha256Hex(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
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

    private sealed class RecordingGoogleWalletService : IGoogleWalletService
    {
        public string? PatchedBusinessName { get; private set; }

        public string? PatchedProgramName { get; private set; }

        public string? PatchedPrimaryColor { get; private set; }

        public int? PatchedCurrentStamps { get; private set; }

        public Task<GoogleWalletIssueResult> IssueSaveLinkAsync(
            LoyaltyCard card,
            Client client,
            Business business,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new GoogleWalletIssueResult(
                $"recording-google-{card.Id:N}",
                $"https://pay.google.com/gp/v/save/recording-{card.Id:N}"));
        }

        public Task PatchStampStateAsync(
            LoyaltyCard card,
            Client client,
            Business business,
            CancellationToken cancellationToken = default)
        {
            PatchedBusinessName = business.DisplayName;
            PatchedProgramName = business.ProgramName;
            PatchedPrimaryColor = business.PrimaryColor;
            PatchedCurrentStamps = card.CurrentStamps;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingAppleWalletService : IAppleWalletService
    {
        public int? NotifiedCurrentStamps { get; private set; }

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
            NotifiedCurrentStamps = card.CurrentStamps;
            return Task.CompletedTask;
        }
    }

    private sealed class UnavailableRewardRedemptionRepository : IRewardRedemptionRepository
    {
        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task AddAsync(RewardRedemptionRecord record, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Unavailable repository should not add records.");
        }

        public Task<IReadOnlyList<RewardRedemptionRecord>> ListRecentByCardIdAsync(
            Guid cardId,
            int limit,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<RewardRedemptionRecord>>([]);
        }
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
