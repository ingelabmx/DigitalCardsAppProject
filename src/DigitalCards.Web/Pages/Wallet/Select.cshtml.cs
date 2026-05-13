using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Wallet;

[EnableRateLimiting(SecurityRateLimitPolicyNames.WalletPublic)]
public sealed class SelectModel : PageModel
{
    private readonly DigitalCardsAppService _appService;

    public SelectModel(DigitalCardsAppService appService)
    {
        _appService = appService;
    }

    public WalletLandingDto? Landing { get; private set; }

    public async Task OnGetAsync(string token, CancellationToken cancellationToken)
    {
        Landing = await _appService.GetWalletLandingAsync(token, cancellationToken);
    }
}
