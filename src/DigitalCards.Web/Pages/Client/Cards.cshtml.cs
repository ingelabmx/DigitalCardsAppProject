using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Client;

[Authorize(Policy = ClientAuth.Policy)]
public sealed class CardsModel : PageModel
{
    private readonly DigitalCardsAppService _appService;

    public CardsModel(DigitalCardsAppService appService)
    {
        _appService = appService;
    }

    public IReadOnlyList<ClientLoyaltyCardDto> Cards { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            Cards = await _appService.GetClientDashboardCardsAsync(
                ClientAuth.GetClientId(User),
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            Cards = [];
        }
    }
}
