using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Client;

public sealed class CardsModel : PageModel
{
    private readonly DigitalCardsAppService _appService;

    public CardsModel(DigitalCardsAppService appService)
    {
        _appService = appService;
    }

    [BindProperty(SupportsGet = true)]
    public string UserName { get; set; } = string.Empty;

    public IReadOnlyList<LoyaltyCardDto> Cards { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(UserName))
        {
            return;
        }

        try
        {
            Cards = await _appService.GetClientCardsAsync(UserName, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            Cards = [];
        }
    }
}

