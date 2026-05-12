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
    private readonly PilotAccessService _pilotAccess;

    public EnrollModel(
        DigitalCardsAppService appService,
        IConfiguration configuration,
        PilotAccessService pilotAccess)
    {
        _appService = appService;
        _configuration = configuration;
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
                ModelState.AddModelError(string.Empty, clientAccess.Message!);
                return Page();
            }

            var businessId = BusinessAuth.GetBusinessId(User);
            Result = await _appService.EnrollClientAsync(
                new EnrollClientCommand(businessId, Input.UserNameOrEmail, GetBaseUrl()),
                cancellationToken);

            return Page();
        }
        catch (InvalidOperationException ex)
        {
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
