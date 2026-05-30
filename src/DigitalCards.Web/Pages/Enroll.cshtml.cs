using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Pilot;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessEntity = DigitalCards.Domain.Business;

namespace DigitalCards.Web.Pages;

[EnableRateLimiting(SecurityRateLimitPolicyNames.PublicWrite)]
public sealed class EnrollModel : PageModel
{
    private const string ConsentPolicyVersion = "privacy-2026-05";
    private readonly IBusinessEnrollmentLinkService _businessEnrollmentLinks;
    private readonly IBusinessRepository _businesses;
    private readonly IBusinessBrandingRepository _businessBranding;
    private readonly DigitalCardsAppService _appService;
    private readonly PilotAccessService _pilotAccess;
    private readonly IConfiguration _configuration;
    private BusinessEntity? _business;
    private DigitalCards.Domain.BusinessBranding? _branding;

    public EnrollModel(
        IBusinessEnrollmentLinkService businessEnrollmentLinks,
        IBusinessRepository businesses,
        IBusinessBrandingRepository businessBranding,
        DigitalCardsAppService appService,
        PilotAccessService pilotAccess,
        IConfiguration configuration)
    {
        _businessEnrollmentLinks = businessEnrollmentLinks;
        _businesses = businesses;
        _businessBranding = businessBranding;
        _appService = appService;
        _pilotAccess = pilotAccess;
        _configuration = configuration;
    }

    [BindProperty(SupportsGet = true)]
    public string BusinessToken { get; set; } = string.Empty;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string BusinessName => !string.IsNullOrWhiteSpace(_branding?.PublicName)
        ? _branding.PublicName
        : _business?.DisplayName ?? "Programa";

    public string ProgramName => !string.IsNullOrWhiteSpace(_branding?.ProgramName)
        ? _branding.ProgramName
        : "Tarjeta de lealtad";

    public string ProgramDescription => !string.IsNullOrWhiteSpace(_branding?.ProgramDescription)
        ? _branding.ProgramDescription
        : "Registra tus datos para recibir tu link Wallet y empezar a acumular sellos.";

    public string? LogoPath => !string.IsNullOrWhiteSpace(_branding?.LogoPath)
        ? _branding.LogoPath
        : _business?.LogoPath;

    public string PrimaryColor => !string.IsNullOrWhiteSpace(_branding?.PrimaryColor)
        ? _branding.PrimaryColor
        : "#2a3547";

    public string SecondaryColor => !string.IsNullOrWhiteSpace(_branding?.SecondaryColor)
        ? _branding.SecondaryColor
        : "#5d87ff";

    public string BusinessEmail => _business?.Email ?? string.Empty;

    public string BusinessInitials => new string(BusinessName
        .Where(char.IsLetterOrDigit)
        .Take(2)
        .DefaultIfEmpty('P')
        .ToArray()).ToUpperInvariant();

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

        if (!Input.AcceptTerms)
        {
            ModelState.AddModelError("Input.AcceptTerms", "Debes aceptar terminos y privacidad para registrarte.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var userName = ClientUserNameNormalizer.NormalizeUserName(Input.UserName);
            var client = await _appService.RegisterClientAsync(
                new RegisterClientCommand(
                    userName,
                    Input.FirstName,
                    Input.LastName,
                    Input.Email,
                    Input.Password),
                cancellationToken);

            var enrollment = await _appService.EnrollClientAsync(
                new EnrollClientCommand(_business!.Id, client.UserName, GetBaseUrl()),
                cancellationToken);
            await _appService.RecordClientConsentAsync(
                new RecordClientConsentCommand(
                    client.Id,
                    _business.Id,
                    ConsentPolicyVersion,
                    "PublicBusinessEnrollment"),
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

        _branding = await _businessBranding.FindByBusinessIdAsync(_business.Id, cancellationToken);
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
        [Display(Name = "Crea un usuario unico")]
        [Required]
        [RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "El usuario solo puede usar letras y numeros, sin espacios.")]
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

        [Display(Name = "Acepto terminos y privacidad")]
        public bool AcceptTerms { get; set; }
    }
}
