using DigitalCards.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalCards.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddDigitalCardsApplication(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher<AdminPasswordHashSubject>, PasswordHasher<AdminPasswordHashSubject>>();
        services.AddSingleton<IPasswordHasher<BusinessPasswordHashSubject>, PasswordHasher<BusinessPasswordHashSubject>>();
        services.AddScoped<AdminAppService>();
        services.AddScoped<DigitalCardsAppService>();
        return services;
    }
}
