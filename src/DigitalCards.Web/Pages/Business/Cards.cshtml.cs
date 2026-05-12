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

    public string? ClientPilotBlockMessage { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? ResentEnrollmentUrl { get; private set; }

    public bool IsPilotBlocked => PilotBlockMessage is not null;

    public bool IsClientPilotBlocked => ClientPilotBlockMessage is not null;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!SetPilotBusinessBlock())
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

        await SetClientPilotBlockAsync(Detail.Client.Email, cancellationToken);
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

    private async Task<bool> ValidateOperationAsync(CancellationToken cancellationToken)
    {
        if (!SetPilotBusinessBlock())
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

        if (!await SetClientPilotBlockAsync(Detail.Client.Email, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, ClientPilotBlockMessage!);
            await LoadSearchResultsAsync(cancellationToken);
            return false;
        }

        return true;
    }

    private bool SetPilotBusinessBlock()
    {
        var access = _pilotAccess.CheckAuthenticatedBusiness(User);
        if (!access.IsAllowed)
        {
            PilotBlockMessage = access.Message;
            return false;
        }

        PilotBlockMessage = null;
        return true;
    }

    private async Task<bool> SetClientPilotBlockAsync(string clientEmail, CancellationToken cancellationToken)
    {
        var access = await _pilotAccess.CheckClientAsync(clientEmail, cancellationToken);
        if (!access.IsAllowed)
        {
            ClientPilotBlockMessage = access.Message;
            return false;
        }

        ClientPilotBlockMessage = null;
        return true;
    }

    private string GetBaseUrl()
    {
        return EnrollmentBaseUrlResolver.Resolve(
            _configuration["DigitalCards:PublicBaseUrl"],
            Request.Scheme,
            Request.Host);
    }
}
