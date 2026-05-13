using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Pilot;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessEntity = DigitalCards.Domain.Business;

namespace DigitalCards.Web.Pages;

public sealed class EnrollModel : PageModel
{
    private readonly IBusinessEnrollmentLinkService _businessEnrollmentLinks;
    private readonly IBusinessRepository _businesses;
    private readonly DigitalCardsAppService _appService;
    private readonly PilotAccessService _pilotAccess;
    private readonly IConfiguration _configuration;
    private BusinessEntity? _business;

    public EnrollModel(
        IBusinessEnrollmentLinkService businessEnrollmentLinks,
        IBusinessRepository businesses,
        DigitalCardsAppService appService,
        PilotAccessService pilotAccess,
        IConfiguration configuration)
    {
        _businessEnrollmentLinks = businessEnrollmentLinks;
        _businesses = businesses;
        _appService = appService;
        _pilotAccess = pilotAccess;
        _configuration = configuration;
    }

    [BindProperty(SupportsGet = true)]
    public string BusinessToken { get; set; } = string.Empty;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string BusinessName => _business?.DisplayName ?? "Programa";

    public bool IsUnavailable { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? WalletLink { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadBusinessAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!await LoadBusinessAsync(cancellationToken))
        {
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var client = await _appService.RegisterClientAsync(
                new RegisterClientCommand(
                    Input.UserName,
                    Input.FirstName,
                    Input.LastName,
                    Input.Email,
                    Input.Password),
                cancellationToken);

            var enrollment = await _appService.EnrollClientAsync(
                new EnrollClientCommand(_business!.Id, client.UserName, GetBaseUrl()),
                cancellationToken);

            WalletLink = enrollment.EnrollmentUrl;
            StatusMessage = "Registro completado. Te enviamos el link para agregar tu tarjeta a Wallet.";
            ModelState.Clear();
            Input = new InputModel();
            return Page();
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return Page();
        }
    }

    private async Task<bool> LoadBusinessAsync(CancellationToken cancellationToken)
    {
        var businessId = await _businessEnrollmentLinks.ResolveBusinessIdAsync(
            BusinessToken,
            cancellationToken);
        if (businessId is null)
        {
            IsUnavailable = true;
            return false;
        }

        _business = await _businesses.FindByIdAsync(businessId.Value, cancellationToken);
        if (_business is null)
        {
            IsUnavailable = true;
            return false;
        }

        var access = await _pilotAccess.CheckBusinessAsync(
            _business.Id,
            _business.Email,
            cancellationToken);
        if (!access.IsAllowed)
        {
            IsUnavailable = true;
            return false;
        }

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
        [Display(Name = "Usuario")]
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Display(Name = "Nombre")]
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Apellido")]
        [Required]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Correo")]
        [EmailAddress]
        [Required]
        public string Email { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Contrasena")]
        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;
    }
}
