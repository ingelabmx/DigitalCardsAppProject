using DigitalCards.Application.Abstractions;
using DigitalCards.Infrastructure.Email;
using DigitalCards.Infrastructure.LegacySync;
using DigitalCards.Infrastructure.Persistence;
using DigitalCards.Infrastructure.Persistence.MySql;
using DigitalCards.Infrastructure.Time;
using DigitalCards.Infrastructure.Wallets;
using DigitalCards.Infrastructure.WalletLinks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalCards.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDigitalCardsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DigitalCardsInfrastructureOptions>(
            configuration.GetSection(DigitalCardsInfrastructureOptions.SectionName));
        services.Configure<GoogleWalletOptions>(
            configuration.GetSection(GoogleWalletOptions.SectionName));
        services.Configure<AppleWalletOptions>(
            configuration.GetSection(AppleWalletOptions.SectionName));
        services.Configure<SmtpEmailOptions>(
            configuration.GetSection(SmtpEmailOptions.SectionName));
        services.Configure<LegacyWalletSyncOptions>(
            configuration.GetSection(LegacyWalletSyncOptions.SectionName));
        services.Configure<WalletLinkOptions>(
            configuration.GetSection(WalletLinkOptions.SectionName));

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<AppleWalletPassPackageBuilder>();
        services.AddScoped<IWalletLinkTokenService, WalletLinkTokenService>();

        var options = configuration
            .GetSection(DigitalCardsInfrastructureOptions.SectionName)
            .Get<DigitalCardsInfrastructureOptions>() ?? new DigitalCardsInfrastructureOptions();
        var googleWalletOptions = configuration
            .GetSection(GoogleWalletOptions.SectionName)
            .Get<GoogleWalletOptions>() ?? new GoogleWalletOptions();
        var appleWalletOptions = configuration
            .GetSection(AppleWalletOptions.SectionName)
            .Get<AppleWalletOptions>() ?? new AppleWalletOptions();
        var emailOptions = configuration
            .GetSection(SmtpEmailOptions.SectionName)
            .Get<SmtpEmailOptions>() ?? new SmtpEmailOptions();
        var legacySyncOptions = configuration
            .GetSection(LegacyWalletSyncOptions.SectionName)
            .Get<LegacyWalletSyncOptions>() ?? new LegacyWalletSyncOptions();

        var providers = DigitalCardsIntegrationConfigurationValidator.Validate(
            configuration,
            options,
            googleWalletOptions,
            appleWalletOptions,
            emailOptions);
        DigitalCardsIntegrationConfigurationValidator.ValidateLegacyWalletSync(
            legacySyncOptions,
            providers.PersistenceProvider);

        if (string.Equals(providers.PersistenceProvider, "MySql", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton(new MySqlConnectionFactory(providers.DigitalCardsConnectionString!));
            services.AddScoped<IClientRepository, MySqlClientRepository>();
            services.AddScoped<IBusinessRepository, MySqlBusinessRepository>();
            services.AddScoped<IBusinessCredentialRepository, MySqlBusinessCredentialRepository>();
            services.AddScoped<ILoyaltyCardRepository, MySqlLoyaltyCardRepository>();
            services.AddScoped<IAppleWalletPassRepository, MySqlAppleWalletPassRepository>();
            services.AddScoped<IWalletLinkTokenRepository, MySqlWalletLinkTokenRepository>();
            services.AddScoped<IStampLedgerRepository, MySqlStampLedgerRepository>();
            services.AddScoped<ILegacyWalletSyncRepository, MySqlLegacyWalletSyncRepository>();
        }
        else
        {
            services.AddSingleton<InMemoryDigitalCardsStore>();
            services.AddScoped<IClientRepository, InMemoryClientRepository>();
            services.AddScoped<IBusinessRepository, InMemoryBusinessRepository>();
            services.AddScoped<IBusinessCredentialRepository, InMemoryBusinessCredentialRepository>();
            services.AddScoped<ILoyaltyCardRepository, InMemoryLoyaltyCardRepository>();
            services.AddScoped<IAppleWalletPassRepository, InMemoryAppleWalletPassRepository>();
            services.AddScoped<IWalletLinkTokenRepository, InMemoryWalletLinkTokenRepository>();
            services.AddScoped<IStampLedgerRepository, InMemoryStampLedgerRepository>();
        }

        if (legacySyncOptions.Enabled)
        {
            services.AddScoped<LegacyWalletSyncProcessor>();
            services.AddHostedService<LegacyWalletSyncWorker>();
        }

        services.AddSingleton<FakeWalletEmailOutbox>();
        services.AddSingleton<IWalletEmailOutbox>(provider => provider.GetRequiredService<FakeWalletEmailOutbox>());

        if (string.Equals(providers.EmailProvider, "Fake", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IEmailSender>(provider => provider.GetRequiredService<FakeWalletEmailOutbox>());
        }
        else if (string.Equals(providers.EmailProvider, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }

        if (string.Equals(providers.GoogleWalletProvider, "Fake", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IGoogleWalletService, FakeGoogleWalletService>();
        }
        else if (string.Equals(providers.GoogleWalletProvider, "Google", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IGoogleWalletService, GoogleWalletService>();
        }

        if (string.Equals(providers.AppleWalletProvider, "Fake", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IAppleWalletService, FakeAppleWalletService>();
            services.AddScoped<IAppleWalletPushSender, FakeAppleWalletPushSender>();
        }
        else if (string.Equals(providers.AppleWalletProvider, "Apple", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IAppleWalletService, AppleWalletService>();
            services.AddScoped<IAppleWalletPushSender, AppleWalletPushSender>();
        }

        return services;
    }
}
