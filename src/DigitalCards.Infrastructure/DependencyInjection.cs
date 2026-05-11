using DigitalCards.Application.Abstractions;
using DigitalCards.Infrastructure.Email;
using DigitalCards.Infrastructure.Persistence;
using DigitalCards.Infrastructure.Persistence.MySql;
using DigitalCards.Infrastructure.Time;
using DigitalCards.Infrastructure.Wallets;
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

        services.AddSingleton<IClock, SystemClock>();

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

        var providers = DigitalCardsIntegrationConfigurationValidator.Validate(
            configuration,
            options,
            googleWalletOptions,
            appleWalletOptions,
            emailOptions);

        if (string.Equals(providers.PersistenceProvider, "MySql", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton(new MySqlConnectionFactory(providers.DigitalCardsConnectionString!));
            services.AddScoped<IClientRepository, MySqlClientRepository>();
            services.AddScoped<IBusinessRepository, MySqlBusinessRepository>();
            services.AddScoped<ILoyaltyCardRepository, MySqlLoyaltyCardRepository>();
        }
        else
        {
            services.AddSingleton<InMemoryDigitalCardsStore>();
            services.AddScoped<IClientRepository, InMemoryClientRepository>();
            services.AddScoped<IBusinessRepository, InMemoryBusinessRepository>();
            services.AddScoped<ILoyaltyCardRepository, InMemoryLoyaltyCardRepository>();
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
        }

        return services;
    }
}
