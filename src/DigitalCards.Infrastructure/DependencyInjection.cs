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
        services.Configure<SmtpEmailOptions>(
            configuration.GetSection(SmtpEmailOptions.SectionName));

        services.AddSingleton<IClock, SystemClock>();

        var options = configuration
            .GetSection(DigitalCardsInfrastructureOptions.SectionName)
            .Get<DigitalCardsInfrastructureOptions>() ?? new DigitalCardsInfrastructureOptions();
        var googleWalletOptions = configuration
            .GetSection(GoogleWalletOptions.SectionName)
            .Get<GoogleWalletOptions>() ?? new GoogleWalletOptions();
        var emailOptions = configuration
            .GetSection(SmtpEmailOptions.SectionName)
            .Get<SmtpEmailOptions>() ?? new SmtpEmailOptions();

        if (string.Equals(options.PersistenceProvider, "MySql", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration.GetConnectionString("DigitalCards")
                ?? throw new InvalidOperationException("ConnectionStrings:DigitalCards is required when DigitalCards:PersistenceProvider is MySql.");

            services.AddSingleton(new MySqlConnectionFactory(connectionString));
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

        var emailProvider = ResolveProvider(emailOptions.Provider, "Fake");
        if (string.Equals(emailProvider, "Fake", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IEmailSender>(provider => provider.GetRequiredService<FakeWalletEmailOutbox>());
        }
        else if (string.Equals(emailProvider, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            throw new InvalidOperationException("DigitalCards:Email:Provider must be Fake or Smtp.");
        }

        var googleWalletProvider = ResolveProvider(
            googleWalletOptions.Provider,
            options.UseFakeIntegrations ? "Fake" : "Google");
        if (string.Equals(googleWalletProvider, "Fake", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IGoogleWalletService, FakeGoogleWalletService>();
        }
        else if (string.Equals(googleWalletProvider, "Google", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IGoogleWalletService, GoogleWalletService>();
        }
        else
        {
            throw new InvalidOperationException("DigitalCards:GoogleWallet:Provider must be Fake or Google.");
        }

        return services;
    }

    private static string ResolveProvider(string? configuredProvider, string defaultProvider)
    {
        return string.IsNullOrWhiteSpace(configuredProvider)
            ? defaultProvider
            : configuredProvider.Trim();
    }
}
