using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Wallet;

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

