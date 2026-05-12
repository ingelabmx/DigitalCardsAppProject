using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web;
using DigitalCards.Web.Pilot;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace DigitalCards.Web.Pages.Business;

[Authorize(Policy = BusinessAuth.Policy)]
public sealed class EnrollModel : PageModel
{
    private readonly DigitalCardsAppService _appService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EnrollModel> _logger;
    private readonly PilotAccessService _pilotAccess;

    public EnrollModel(
        DigitalCardsAppService appService,
        IConfiguration configuration,
        ILogger<EnrollModel> logger,
        PilotAccessService pilotAccess)
    {
        _appService = appService;
        _configuration = configuration;
        _logger = logger;
        _pilotAccess = pilotAccess;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public EnrollClientResult? Result { get; private set; }

    public string? PilotBlockMessage { get; private set; }

    public bool IsPilotBlocked => PilotBlockMessage is not null;

    public void OnGet()
    {
        SetPilotBusinessBlock();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!SetPilotBusinessBlock())
        {
            _logger.LogWarning(
                "Modern enroll blocked by pilot for business {BusinessId}.",
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
            var clientAccess = await _pilotAccess.CheckClientAsync(Input.UserNameOrEmail, cancellationToken);
            if (!clientAccess.IsAllowed)
            {
                _logger.LogWarning(
                    "Modern enroll blocked by client pilot allowlist for business {BusinessId}.",
                    BusinessAuth.GetBusinessId(User));
                ModelState.AddModelError(string.Empty, clientAccess.Message!);
                return Page();
            }

            var businessId = BusinessAuth.GetBusinessId(User);
            Result = await _appService.EnrollClientAsync(
                new EnrollClientCommand(businessId, Input.UserNameOrEmail, GetBaseUrl()),
                cancellationToken);

            _logger.LogInformation(
                "Modern enroll completed for business {BusinessId} card {CardId}.",
                businessId,
                Result.Card.Id);
            return Page();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                "Modern enroll failed for business {BusinessId}: {FailureReason}.",
                BusinessAuth.GetBusinessId(User),
                ex.Message);
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
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

    private string GetBaseUrl()
    {
        return EnrollmentBaseUrlResolver.Resolve(
            _configuration["DigitalCards:PublicBaseUrl"],
            Request.Scheme,
            Request.Host);
    }

    public sealed class InputModel
    {
        [Display(Name = "Usuario o correo del cliente")]
        [Required]
        public string UserNameOrEmail { get; set; } = string.Empty;
    }
}
