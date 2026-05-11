using DigitalCards.Application;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Domain;
using DigitalCards.Infrastructure;
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
        Assert.IsType<MySqlLoyaltyCardRepository>(provider.GetRequiredService<ILoyaltyCardRepository>());
        Assert.IsType<MySqlAppleWalletPassRepository>(provider.GetRequiredService<IAppleWalletPassRepository>());
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

        var outbox = provider.GetRequiredService<IWalletEmailOutbox>();
        var messages = await outbox.ListAsync();

        Assert.Single(messages);
        Assert.Equal("maria@example.test", messages[0].To);

        var google = await app.SelectGoogleWalletAsync(enrollment.Card.EnrollmentToken);

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

        var result = await app.SelectAppleWalletAsync(enrollment.Card.EnrollmentToken);

        Assert.NotNull(result);
        Assert.Equal(AppleWalletIssueStatus.Pending, result!.Status);
        Assert.Null(result.DownloadUrl);
        Assert.Null(result.SerialNumber);
        Assert.Contains(".pkpass", result.Message);
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
}
