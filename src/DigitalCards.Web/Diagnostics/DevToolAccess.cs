using System.Security.Claims;
using DigitalCards.Web.Security;
using Microsoft.Extensions.Hosting;

namespace DigitalCards.Web.Diagnostics;

public static class DevToolAccess
{
    public const string EnableDevOutboxConfigurationKey = "DigitalCards:Diagnostics:EnableDevOutbox";

    public static bool IsDevOutboxEnabled(IWebHostEnvironment environment, IConfiguration configuration)
    {
        return environment.IsDevelopment()
            || configuration.GetValue<bool>(EnableDevOutboxConfigurationKey);
    }

    public static bool CanAccessDevOutbox(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ClaimsPrincipal user)
    {
        return environment.IsDevelopment()
            || (configuration.GetValue<bool>(EnableDevOutboxConfigurationKey)
                && user.HasClaim(claim => claim.Type == AdminAuth.AdminUserIdClaim));
    }

    public static bool ShouldRenderDevOutboxLink(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ClaimsPrincipal user)
    {
        return environment.IsDevelopment()
            || (configuration.GetValue<bool>(EnableDevOutboxConfigurationKey)
                && user.HasClaim(claim => claim.Type == AdminAuth.AdminUserIdClaim));
    }
}
