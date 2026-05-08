using DigitalCards.Application.Abstractions;
using DigitalCards.Infrastructure.Email;
using DigitalCards.Infrastructure.Persistence;
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

        services.AddSingleton<InMemoryDigitalCardsStore>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IClientRepository, InMemoryClientRepository>();
        services.AddScoped<IBusinessRepository, InMemoryBusinessRepository>();
        services.AddScoped<ILoyaltyCardRepository, InMemoryLoyaltyCardRepository>();
        services.AddSingleton<FakeWalletEmailOutbox>();
        services.AddSingleton<IWalletEmailOutbox>(provider => provider.GetRequiredService<FakeWalletEmailOutbox>());
        services.AddScoped<IEmailSender>(provider => provider.GetRequiredService<FakeWalletEmailOutbox>());
        services.AddScoped<IGoogleWalletService, FakeGoogleWalletService>();

        return services;
    }
}

