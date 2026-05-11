using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Wallet;

public sealed class AppleModel : PageModel
{
    private readonly DigitalCardsAppService _appService;

    public AppleModel(DigitalCardsAppService appService)
    {
        _appService = appService;
    }

    public string Token { get; private set; } = string.Empty;

    public AppleWalletIssueResult? Result { get; private set; }

    public async Task OnGetAsync(string token, CancellationToken cancellationToken)
    {
        Token = token;
        Result = await _appService.SelectAppleWalletAsync(token, cancellationToken);
    }
}
