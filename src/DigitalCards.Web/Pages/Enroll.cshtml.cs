using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
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
    private readonly IClientRepository _clients;
    private readonly PilotAccessService _pilotAccess;
    private readonly IConfiguration _configuration;
    private BusinessEntity? _business;
    private DigitalCards.Domain.BusinessBranding? _branding;

    public EnrollModel(
        IBusinessEnrollmentLinkService businessEnrollmentLinks,
        IBusinessRepository businesses,
        IBusinessBrandingRepository businessBranding,
        DigitalCardsAppService appService,
        IClientRepository clients,
        PilotAccessService pilotAccess,
        IConfiguration configuration)
    {
        _businessEnrollmentLinks = businessEnrollmentLinks;
        _businesses = businesses;
        _businessBranding = businessBranding;
        _appService = appService;
        _clients = clients;
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

    public bool ShowExistingForm { get; private set; }

    public string? ExistingFormError { get; private set; }

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
            var userName = await GenerateUserNameAsync(Input.FirstName, Input.LastName, cancellationToken);
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

            return Redirect(enrollment.EnrollmentUrl);
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostEnrollExistingAsync(
        string usernameOrEmail,
        string password,
        CancellationToken cancellationToken)
    {
        if (!await LoadBusinessAsync(cancellationToken))
            return Page();

        var client = await _appService.LoginClientAsync(
            new ClientLoginCommand(usernameOrEmail, password),
            cancellationToken);

        if (client is null)
        {
            ShowExistingForm = true;
            ExistingFormError = "Usuario, correo o contraseña incorrectos.";
            return Page();
        }

        var enrollment = await _appService.EnrollClientAsync(
            new EnrollClientCommand(_business!.Id, client.UserName, GetBaseUrl()),
            cancellationToken);

        await _appService.RecordClientConsentAsync(
            new RecordClientConsentCommand(
                client.Id,
                _business.Id,
                ConsentPolicyVersion,
                "PublicBusinessEnrollmentExisting"),
            cancellationToken);

        return Redirect(enrollment.EnrollmentUrl);
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

    private async Task<string> GenerateUserNameAsync(string firstName, string lastName, CancellationToken ct)
    {
        var initial = NormalizeToAscii(firstName.Trim());
        initial = initial.Length > 0 ? initial[..1] : "";
        var surname = NormalizeToAscii(lastName.Trim());
        var baseCandidate = initial + surname;
        if (string.IsNullOrEmpty(baseCandidate))
            baseCandidate = "cliente";

        if (!await _clients.UserNameOrEmailExistsAsync(baseCandidate, ct))
            return baseCandidate;

        for (var i = 1; i <= 999; i++)
        {
            var candidate = $"{baseCandidate}{i}";
            if (!await _clients.UserNameOrEmailExistsAsync(candidate, ct))
                return candidate;
        }
        return (baseCandidate + Guid.NewGuid().ToString("N"))[..20];
    }

    private static string NormalizeToAscii(string input)
    {
        var sb = new StringBuilder();
        foreach (var c in input.Normalize(NormalizationForm.FormD))
        {
            var lower = char.ToLowerInvariant(c);
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark
                && lower is >= 'a' and <= 'z')
            {
                sb.Append(lower);
            }
        }
        return sb.ToString();
    }

    public sealed class InputModel
    {
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
