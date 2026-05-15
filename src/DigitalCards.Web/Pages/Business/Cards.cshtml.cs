using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web;
using DigitalCards.Web.Pilot;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Business;

[Authorize(Policy = BusinessAuth.Policy)]
public sealed class CardsModel : PageModel
{
    private readonly DigitalCardsAppService _appService;
    private readonly IConfiguration _configuration;
    private readonly PilotAccessService _pilotAccess;

    public CardsModel(
        DigitalCardsAppService appService,
        IConfiguration configuration,
        PilotAccessService pilotAccess)
    {
        _appService = appService;
        _configuration = configuration;
        _pilotAccess = pilotAccess;
    }

    [BindProperty(SupportsGet = true)]
    [Display(Name = "Usuario o correo")]
    public string Query { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public Guid? CardId { get; set; }

    public IReadOnlyList<BusinessCardDto> Results { get; private set; } = [];

    public BusinessCardDto? Detail { get; private set; }

    public string? PilotBlockMessage { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? ResentEnrollmentUrl { get; private set; }

    public bool IsPilotBlocked => PilotBlockMessage is not null;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!await SetPilotBusinessBlockAsync(cancellationToken))
        {
            return Page();
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostStampAsync(Guid cardId, CancellationToken cancellationToken)
    {
        CardId = cardId;

        if (!await ValidateOperationAsync(cancellationToken))
        {
            return Page();
        }

        var businessId = BusinessAuth.GetBusinessId(User);
        if (IsRewardReady(Detail!))
        {
            ModelState.AddModelError(string.Empty, "La tarjeta ya esta completa. Confirma el canje de recompensa.");
            await LoadSearchResultsAsync(cancellationToken);
            return Page();
        }

        Detail = await _appService.AddStampToCardAsync(businessId, cardId, cancellationToken);
        if (Detail is null)
        {
            ModelState.AddModelError(string.Empty, "La tarjeta no existe para este negocio.");
            await LoadSearchResultsAsync(cancellationToken);
            return Page();
        }

        StatusMessage = $"Sello agregado a {Detail.Client.UserName}.";
        await LoadSearchResultsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostRedeemAsync(Guid cardId, CancellationToken cancellationToken)
    {
        CardId = cardId;

        if (!await ValidateOperationAsync(cancellationToken))
        {
            return Page();
        }

        var result = await _appService.RedeemRewardAsync(
            BusinessAuth.GetBusinessId(User),
            cardId,
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo canjear la recompensa.");
            await LoadSearchResultsAsync(cancellationToken);
            return Page();
        }

        Detail = result.Card;
        StatusMessage = result.HasWalletWarning
            ? result.ErrorMessage
            : "Recompensa canjeada. La tarjeta inicio un nuevo ciclo con 0 sellos.";
        await LoadSearchResultsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostResendAsync(Guid cardId, CancellationToken cancellationToken)
    {
        CardId = cardId;

        if (!await ValidateOperationAsync(cancellationToken))
        {
            return Page();
        }

        var businessId = BusinessAuth.GetBusinessId(User);
        var result = await _appService.ResendWalletEmailAsync(
            businessId,
            cardId,
            GetBaseUrl(),
            cancellationToken);

        if (result is null)
        {
            ModelState.AddModelError(string.Empty, "La tarjeta no existe para este negocio.");
            await LoadSearchResultsAsync(cancellationToken);
            return Page();
        }

        Detail = result.Card;
        ResentEnrollmentUrl = result.EnrollmentUrl;
        StatusMessage = $"Correo reenviado a {Detail.Client.Email}.";
        await LoadSearchResultsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid cardId, CancellationToken cancellationToken)
    {
        return await SetCardActiveAsync(cardId, isActive: false, "Tarjeta desactivada para este negocio.", cancellationToken);
    }

    public async Task<IActionResult> OnPostReactivateAsync(Guid cardId, CancellationToken cancellationToken)
    {
        return await SetCardActiveAsync(cardId, isActive: true, "Tarjeta reactivada para este negocio.", cancellationToken);
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid cardId, string confirmation, CancellationToken cancellationToken)
    {
        CardId = cardId;

        if (!await ValidateOperationAsync(cancellationToken, allowInactive: true))
        {
            return Page();
        }

        var expected = Detail!.Client.UserName;
        if (!string.Equals(confirmation?.Trim(), expected, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, $"Escribe exactamente {expected} para eliminar la tarjeta.");
            await LoadSearchResultsAsync(cancellationToken);
            return Page();
        }

        var result = await _appService.DeleteBusinessCardAsync(
            BusinessAuth.GetBusinessId(User),
            cardId,
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo eliminar la tarjeta.");
            await LoadSearchResultsAsync(cancellationToken);
            return Page();
        }

        CardId = null;
        Detail = null;
        StatusMessage = "Tarjeta eliminada para este negocio. La cuenta global del cliente se conserva.";
        await LoadSearchResultsAsync(cancellationToken);
        return Page();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        await LoadSearchResultsAsync(cancellationToken);

        if (CardId is null)
        {
            return;
        }

        Detail = await _appService.GetBusinessCardDetailAsync(
            BusinessAuth.GetBusinessId(User),
            CardId.Value,
            cancellationToken);

        if (Detail is null)
        {
            ModelState.AddModelError(string.Empty, "La tarjeta no existe para este negocio.");
            return;
        }

    }

    private async Task LoadSearchResultsAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Query))
        {
            Results = [];
            return;
        }

        Results = await _appService.SearchBusinessCardsAsync(
            BusinessAuth.GetBusinessId(User),
            Query,
            cancellationToken);
    }

    private async Task<IActionResult> SetCardActiveAsync(
        Guid cardId,
        bool isActive,
        string statusMessage,
        CancellationToken cancellationToken)
    {
        CardId = cardId;

        if (!await ValidateOperationAsync(cancellationToken, allowInactive: true))
        {
            return Page();
        }

        var result = await _appService.SetBusinessCardActiveAsync(
            BusinessAuth.GetBusinessId(User),
            cardId,
            isActive,
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo cambiar el estado de la tarjeta.");
            await LoadSearchResultsAsync(cancellationToken);
            return Page();
        }

        Detail = result.Card;
        StatusMessage = statusMessage;
        await LoadSearchResultsAsync(cancellationToken);
        return Page();
    }

    private async Task<bool> ValidateOperationAsync(CancellationToken cancellationToken, bool allowInactive = false)
    {
        if (!await SetPilotBusinessBlockAsync(cancellationToken))
        {
            ModelState.AddModelError(string.Empty, PilotBlockMessage!);
            return false;
        }

        var businessId = BusinessAuth.GetBusinessId(User);
        Detail = await _appService.GetBusinessCardDetailAsync(businessId, CardId!.Value, cancellationToken);
        if (Detail is null)
        {
            ModelState.AddModelError(string.Empty, "La tarjeta no existe para este negocio.");
            await LoadSearchResultsAsync(cancellationToken);
            return false;
        }

        if (!allowInactive && !Detail.IsActive)
        {
            ModelState.AddModelError(string.Empty, "La tarjeta esta desactivada para este negocio.");
            await LoadSearchResultsAsync(cancellationToken);
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

    private string GetBaseUrl()
    {
        return EnrollmentBaseUrlResolver.Resolve(
            _configuration["DigitalCards:PublicBaseUrl"],
            Request.Scheme,
            Request.Host);
    }

    private static bool IsRewardReady(BusinessCardDto card)
    {
        return Math.Min(Math.Max(card.CurrentStamps, 0), Math.Max(1, card.StampGoal)) >= Math.Max(1, card.StampGoal);
    }
}
