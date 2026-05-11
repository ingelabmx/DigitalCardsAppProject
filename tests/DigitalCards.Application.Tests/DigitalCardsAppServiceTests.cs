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
    }

    [Fact]
    public void AddInfrastructure_RegistersFakeGoogleWalletByDefault()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDigitalCardsInfrastructure(new ConfigurationBuilder().Build());

        var provider = services.BuildServiceProvider();

        Assert.IsType<FakeGoogleWalletService>(provider.GetRequiredService<IGoogleWalletService>());
        Assert.IsType<FakeWalletEmailOutbox>(provider.GetRequiredService<IEmailSender>());
    }

    [Fact]
    public void AddInfrastructure_RegistersRealGoogleWalletWhenProviderIsGoogle()
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
                ["DigitalCards:GoogleWallet:Origins:0"] = "https://example.test"
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
                ["DigitalCards:Email:Provider"] = "Smtp"
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
    public async Task SmtpEmailSender_RequiresConfigurationBeforeConnecting()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DigitalCards:Email:Provider"] = "Smtp"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDigitalCardsInfrastructure(configuration);

        var provider = services.BuildServiceProvider();
        var emailSender = provider.GetRequiredService<IEmailSender>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            emailSender.SendWalletEnrollmentAsync(new WalletEnrollmentEmail(
                "maria@example.test",
                "Maria Lopez",
                "Demo Coffee",
                "https://example.test/Wallet/Select/token",
                DateTimeOffset.Parse("2026-05-11T00:00:00Z"))));

        Assert.Contains("DigitalCards:Email:Host", exception.Message);
        Assert.DoesNotContain("Password=", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RealGoogleWallet_RequiresIssuerBeforeCallingGoogle()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DigitalCards:UseFakeIntegrations"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDigitalCardsInfrastructure(configuration);

        var provider = services.BuildServiceProvider();
        var googleWallet = provider.GetRequiredService<IGoogleWalletService>();
        var createdAt = DateTimeOffset.Parse("2026-05-08T00:00:00Z");
        var client = new Client(Guid.NewGuid(), "maria-test", "Maria", "Lopez", "maria@example.test");
        var business = new Business(Guid.NewGuid(), "Demo Business", "demo@example.test", "hash", "logo.jpg");
        var card = new LoyaltyCard(Guid.NewGuid(), client.Id, business.Id, createdAt);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            googleWallet.IssueSaveLinkAsync(card, client, business));

        Assert.Contains("DigitalCards:GoogleWallet:IssuerId", exception.Message);
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
}
