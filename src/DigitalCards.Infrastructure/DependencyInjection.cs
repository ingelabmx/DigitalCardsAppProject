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

        services.AddSingleton<IClock, SystemClock>();

        var options = configuration
            .GetSection(DigitalCardsInfrastructureOptions.SectionName)
            .Get<DigitalCardsInfrastructureOptions>() ?? new DigitalCardsInfrastructureOptions();

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
        services.AddScoped<IEmailSender>(provider => provider.GetRequiredService<FakeWalletEmailOutbox>());

        if (options.UseFakeIntegrations)
        {
            services.AddScoped<IGoogleWalletService, FakeGoogleWalletService>();
        }
        else
        {
            services.AddScoped<IGoogleWalletService, GoogleWalletService>();
        }

        return services;
    }
}
