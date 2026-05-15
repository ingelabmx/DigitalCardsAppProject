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
public sealed class StampModel : PageModel
{
    private readonly DigitalCardsAppService _appService;
    private readonly ILogger<StampModel> _logger;
    private readonly PilotAccessService _pilotAccess;

    public StampModel(
        DigitalCardsAppService appService,
        ILogger<StampModel> logger,
        PilotAccessService pilotAccess)
    {
        _appService = appService;
        _logger = logger;
        _pilotAccess = pilotAccess;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public LoyaltyCardDto? Result { get; private set; }

    public BusinessCardDto? RewardCandidate { get; private set; }

    public RewardRedemptionResult? Redemption { get; private set; }

    public string? PilotBlockMessage { get; private set; }

    public bool IsPilotBlocked => PilotBlockMessage is not null;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await SetPilotBusinessBlockAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!await SetPilotBusinessBlockAsync(cancellationToken))
        {
            _logger.LogWarning(
                "Modern stamp blocked by pilot for business {BusinessId}.",
                BusinessAuth.GetBusinessId(User));
            ModelState.AddModelError(string.Empty, PilotBlockMessage!);
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var businessId = BusinessAuth.GetBusinessId(User);
            RewardCandidate = await _appService.GetBusinessCardForClientAsync(
                businessId,
                Input.UserNameOrEmail,
                cancellationToken);
            if (RewardCandidate is not null && RewardCandidate.RewardReady)
            {
                return Page();
            }

            Result = await _appService.AddStampAsync(
                new AddStampCommand(businessId, Input.UserNameOrEmail),
                cancellationToken);

            _logger.LogInformation(
                "Modern stamp completed for business {BusinessId} card {CardId} current stamps {CurrentStamps} lifetime stamps {LifetimeStamps}.",
                businessId,
                Result.Id,
                Result.CurrentStamps,
                Result.LifetimeStamps);
            if (Result.RewardReady)
            {
                RewardCandidate = await _appService.GetBusinessCardForClientAsync(
                    businessId,
                    Result.ClientUserName,
                    cancellationToken);
            }

            Input = new InputModel();
            ModelState.Clear();
            return Page();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                "Modern stamp failed for business {BusinessId}: {FailureReason}.",
                BusinessAuth.GetBusinessId(User),
                ex.Message);
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostRedeemAsync(Guid cardId, CancellationToken cancellationToken)
    {
        if (!await SetPilotBusinessBlockAsync(cancellationToken))
        {
            ModelState.AddModelError(string.Empty, PilotBlockMessage!);
            return Page();
        }

        Redemption = await _appService.RedeemRewardAsync(
            BusinessAuth.GetBusinessId(User),
            cardId,
            cancellationToken);

        if (!Redemption.Succeeded)
        {
            RewardCandidate = Redemption.Card;
            ModelState.AddModelError(string.Empty, Redemption.ErrorMessage ?? "No se pudo canjear la recompensa.");
            return Page();
        }

        Input = new InputModel();
        ModelState.Clear();
        return Page();
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

    public sealed class InputModel
    {
        [Display(Name = "Usuario o correo del cliente")]
        [Required]
        public string UserNameOrEmail { get; set; } = string.Empty;
    }

}
