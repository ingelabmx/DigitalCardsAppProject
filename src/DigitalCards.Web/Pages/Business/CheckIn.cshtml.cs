using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Pilot;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Business;

[Authorize(Policy = BusinessAuth.Policy)]
public sealed class CheckInModel : PageModel
{
    private readonly DigitalCardsAppService _appService;
    private readonly PilotAccessService _pilotAccess;

    public CheckInModel(
        DigitalCardsAppService appService,
        PilotAccessService pilotAccess)
    {
        _appService = appService;
        _pilotAccess = pilotAccess;
    }

    [BindProperty]
    [Display(Name = "QR, username o correo")]
    public string Query { get; set; } = string.Empty;

    public IReadOnlyList<BusinessCardDto> Results { get; private set; } = [];

    public BusinessCardDto? SelectedCard { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? PilotBlockMessage { get; private set; }

    public bool IsPilotBlocked => PilotBlockMessage is not null;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await SetPilotBusinessBlockAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostLookupAsync(CancellationToken cancellationToken)
    {
        if (!await ValidateBusinessAccessAsync(cancellationToken))
        {
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Query))
        {
            ModelState.AddModelError(nameof(Query), "Captura o escanea el QR del cliente.");
            return Page();
        }

        Results = await _appService.SearchBusinessCardsAsync(
            BusinessAuth.GetBusinessId(User),
            Query,
            cancellationToken);

        SelectedCard = Results.Count == 1 ? Results[0] : null;
        if (Results.Count == 0)
        {
            StatusMessage = "No se encontro una tarjeta para este negocio.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostStampAsync(Guid cardId, CancellationToken cancellationToken)
    {
        if (!await ValidateBusinessAccessAsync(cancellationToken))
        {
            return Page();
        }

        SelectedCard = await _appService.AddStampToCardAsync(
            BusinessAuth.GetBusinessId(User),
            cardId,
            cancellationToken);
        if (SelectedCard is null)
        {
            ModelState.AddModelError(string.Empty, "La tarjeta no existe para este negocio.");
            return Page();
        }

        Query = SelectedCard.Client.UserName;
        Results = [SelectedCard];
        StatusMessage = $"Sello agregado a {SelectedCard.Client.UserName}.";
        return Page();
    }

    private async Task<bool> ValidateBusinessAccessAsync(CancellationToken cancellationToken)
    {
        if (!await SetPilotBusinessBlockAsync(cancellationToken))
        {
            ModelState.AddModelError(string.Empty, PilotBlockMessage!);
            return false;
        }

        return true;
    }

    private async Task<bool> SetPilotBusinessBlockAsync(CancellationToken cancellationToken)
    {
        var access = await _pilotAccess.CheckAuthenticatedBusinessAsync(User, cancellationToken);
        if (!access.IsAllowed)
        {
            PilotBlockMessage = access.Message;
            return false;
        }

        PilotBlockMessage = null;
        return true;
    }
}
