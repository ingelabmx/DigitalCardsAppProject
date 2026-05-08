using DigitalCards.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalCards.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddDigitalCardsApplication(this IServiceCollection services)
    {
        services.AddScoped<DigitalCardsAppService>();
        return services;
    }
}

