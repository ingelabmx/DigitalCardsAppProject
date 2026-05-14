using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages;

public sealed class IndexModel : PageModel
{
    public GatewaySession? AdminSession { get; private set; }

    public GatewaySession? BusinessSession { get; private set; }

    public GatewaySession? ClientSession { get; private set; }

    public async Task OnGetAsync()
    {
        AdminSession = await GetSessionAsync(AdminAuth.Scheme, AdminAuth.AdminNameClaim);
        BusinessSession = await GetSessionAsync(BusinessAuth.Scheme, BusinessAuth.BusinessNameClaim);
        ClientSession = await GetSessionAsync(ClientAuth.Scheme, ClientAuth.ClientNameClaim);
    }

    private async Task<GatewaySession?> GetSessionAsync(string scheme, string displayNameClaim)
    {
        var result = await HttpContext.AuthenticateAsync(scheme);
        if (!result.Succeeded || result.Principal is null)
        {
            return null;
        }

        var displayName = result.Principal.FindFirst(displayNameClaim)?.Value;
        return new GatewaySession(string.IsNullOrWhiteSpace(displayName) ? "sesion activa" : displayName);
    }

    public sealed record GatewaySession(string DisplayName);
}
