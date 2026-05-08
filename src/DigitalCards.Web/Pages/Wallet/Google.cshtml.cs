using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Wallet;

public sealed class GoogleModel : PageModel
{
    private readonly DigitalCardsAppService _appService;

    public GoogleModel(DigitalCardsAppService appService)
    {
        _appService = appService;
    }

    public GoogleWalletIssueResult? Result { get; private set; }

    public async Task OnGetAsync(string token, CancellationToken cancellationToken)
    {
        Result = await _appService.SelectGoogleWalletAsync(token, cancellationToken);
    }
}

