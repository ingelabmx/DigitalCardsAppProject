using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Wallet;

[EnableRateLimiting(SecurityRateLimitPolicyNames.WalletPublic)]
public sealed class GoogleModel : PageModel
{
    private readonly DigitalCardsAppService _appService;

    public GoogleModel(DigitalCardsAppService appService)
    {
        _appService = appService;
    }

    public GoogleWalletIssueResult? Result { get; private set; }

    public WalletLandingDto? Landing { get; private set; }

    public async Task OnGetAsync(string token, CancellationToken cancellationToken)
    {
        Landing = await _appService.GetWalletLandingAsync(token, cancellationToken);
        Result = await _appService.SelectGoogleWalletAsync(token, cancellationToken);
    }
}
