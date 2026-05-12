using DigitalCards.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalCards.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddDigitalCardsApplication(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher<BusinessPasswordHashSubject>, PasswordHasher<BusinessPasswordHashSubject>>();
        services.AddScoped<DigitalCardsAppService>();
        return services;
    }
}
