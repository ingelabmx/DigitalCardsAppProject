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

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 10;

    public IReadOnlyList<BusinessCardDto> Cards { get; private set; } = [];

    public IReadOnlyList<BusinessCardDto> PagedCards { get; private set; } = [];

    public BusinessCardDto? Detail { get; private set; }

    public CardLookupState LookupState { get; private set; } = CardLookupState.Empty;

    public string? PilotBlockMessage { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? ResentEnrollmentUrl { get; private set; }

    public bool IsPilotBlocked => PilotBlockMessage is not null;

    public int TotalPages { get; private set; }

    public int TotalCards => Cards.Count;

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
        if (Detail!.RewardReady)
        {
            ModelState.AddModelError(string.Empty, "La tarjeta ya esta completa. Confirma el canje de recompensa.");
            await LoadAsync(cancellationToken);
            return Page();
        }

        Detail = await _appService.AddStampToCardAsync(businessId, cardId, cancellationToken);
        if (Detail is null)
        {
            ModelState.AddModelError(string.Empty, "La tarjeta no existe para este negocio.");
            await LoadAsync(cancellationToken);
            return Page();
        }

        StatusMessage = $"Sello agregado a {Detail.Client.UserName}.";
        await LoadAsync(cancellationToken);
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
            Detail = result.Card ?? Detail;
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo canjear la recompensa.");
            await LoadAsync(cancellationToken);
            return Page();
        }

        Detail = result.Card;
        StatusMessage = result.HasWalletWarning
            ? result.ErrorMessage
            : "Recompensa canjeada. La tarjeta inicio un nuevo ciclo con 0 sellos.";
        await LoadAsync(cancellationToken);
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
            await LoadAsync(cancellationToken);
            return Page();
        }

        Detail = result.Card;
        ResentEnrollmentUrl = result.EnrollmentUrl;
        StatusMessage = $"Correo reenviado a {Detail.Client.Email}.";
        await LoadAsync(cancellationToken);
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
            await LoadAsync(cancellationToken);
            return Page();
        }

        var result = await _appService.DeleteBusinessCardAsync(
            BusinessAuth.GetBusinessId(User),
            cardId,
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo eliminar la tarjeta.");
            await LoadAsync(cancellationToken);
            return Page();
        }

        CardId = null;
        Detail = null;
        LookupState = CardLookupState.NotFound;
        StatusMessage = "Tarjeta eliminada para este negocio. La cuenta global del cliente se conserva.";
        await LoadAsync(cancellationToken);
        return Page();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        await LoadCardTableAsync(cancellationToken);

        if (CardId is not null)
        {
            Detail = await _appService.GetBusinessCardDetailAsync(
                BusinessAuth.GetBusinessId(User),
                CardId.Value,
                cancellationToken);

            if (Detail is null)
            {
                LookupState = CardLookupState.NotFound;
                ModelState.AddModelError(string.Empty, "La tarjeta no existe para este negocio.");
            }

            return;
        }

        await ResolveQueryToDetailAsync(cancellationToken);
    }

    private async Task ResolveQueryToDetailAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Query))
        {
            LookupState = CardLookupState.Empty;
            return;
        }

        var results = await _appService.SearchBusinessCardsAsync(
            BusinessAuth.GetBusinessId(User),
            Query,
            cancellationToken);

        var exact = results
            .Where(result =>
                string.Equals(result.Client.UserName, Query.Trim(), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(result.Client.Email, Query.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (exact.Length == 1)
        {
            Detail = exact[0];
            CardId = Detail.Id;
            LookupState = CardLookupState.Found;
            return;
        }

        if (results.Count == 1)
        {
            Detail = results[0];
            CardId = Detail.Id;
            LookupState = CardLookupState.Found;
            return;
        }

        LookupState = results.Count == 0
            ? CardLookupState.NotFound
            : CardLookupState.Ambiguous;
    }

    private async Task LoadCardTableAsync(CancellationToken cancellationToken)
    {
        Cards = string.IsNullOrWhiteSpace(Query)
            ? await _appService.ListBusinessCardsAsync(
                BusinessAuth.GetBusinessId(User),
                cancellationToken)
            : await _appService.SearchBusinessCardsAsync(
                BusinessAuth.GetBusinessId(User),
                Query,
                cancellationToken);

        PageSize = NormalizePageSize(PageSize);
        TotalPages = Math.Max(1, (int)Math.Ceiling(Cards.Count / (double)PageSize));
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);
        PagedCards = Cards
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToArray();
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
            await LoadAsync(cancellationToken);
            return Page();
        }

        Detail = result.Card;
        StatusMessage = statusMessage;
        await LoadAsync(cancellationToken);
        return Page();
    }

    private async Task<bool> ValidateOperationAsync(CancellationToken cancellationToken, bool allowInactive = false)
    {
        if (!await SetPilotBusinessBlockAsync(cancellationToken))
        {
            ModelState.AddModelError(string.Empty, PilotBlockMessage!);
            await LoadCardTableAsync(cancellationToken);
            return false;
        }

        var businessId = BusinessAuth.GetBusinessId(User);
        Detail = await _appService.GetBusinessCardDetailAsync(businessId, CardId!.Value, cancellationToken);
        if (Detail is null)
        {
            ModelState.AddModelError(string.Empty, "La tarjeta no existe para este negocio.");
            await LoadCardTableAsync(cancellationToken);
            return false;
        }

        if (!allowInactive && !Detail.IsActive)
        {
            ModelState.AddModelError(string.Empty, "La tarjeta esta desactivada para este negocio.");
            await LoadCardTableAsync(cancellationToken);
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

    public enum CardLookupState
    {
        Empty,
        Found,
        NotFound,
        Ambiguous
    }

    private static int NormalizePageSize(int value)
    {
        return value is 20 or 50 ? value : 10;
    }
}
