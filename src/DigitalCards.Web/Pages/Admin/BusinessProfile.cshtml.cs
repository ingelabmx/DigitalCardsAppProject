using DigitalCards.Application.Models;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Services;
using DigitalCards.Domain;
using DigitalCards.Web.Branding;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Admin;

[Authorize(Policy = AdminAuth.Policy)]
public sealed class BusinessProfileModel : PageModel
{
    private readonly AdminAppService _adminApp;
    private readonly IBusinessEnrollmentLinkService _businessEnrollmentLinks;
    private readonly BusinessLogoUploadService _logoUploads;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BusinessProfileModel> _logger;

    public BusinessProfileModel(
        AdminAppService adminApp,
        IBusinessEnrollmentLinkService businessEnrollmentLinks,
        BusinessLogoUploadService logoUploads,
        IConfiguration configuration,
        ILogger<BusinessProfileModel> logger)
    {
        _adminApp = adminApp;
        _businessEnrollmentLinks = businessEnrollmentLinks;
        _logoUploads = logoUploads;
        _configuration = configuration;
        _logger = logger;
    }

    [BindProperty]
    public ProfileInputModel Input { get; set; } = new();

    [BindProperty]
    public PasswordInputModel PasswordInput { get; set; } = new();

    [BindProperty]
    public BrandingInputModel BrandingInput { get; set; } = new();

    public BusinessProfileDto? Profile { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? GeneratedEnrollmentUrl { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid businessId, CancellationToken cancellationToken)
    {
        return await LoadAsync(businessId, cancellationToken)
            ? Page()
            : NotFound();
    }

    public async Task<IActionResult> OnPostSaveAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var result = await _adminApp.UpdateBusinessProfileAsync(
            new UpdateBusinessProfileCommand(
                businessId,
                AdminAuth.GetAdminUserId(User),
                Input.BusinessName,
                Input.BusinessEmail,
                Input.BusinessLogo,
                Input.IsPilotEnabled,
                Input.Notes,
                Input.ActivationStatus),
            cancellationToken);

        ClearPasswordFields();

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo actualizar el negocio.");
            await LoadAsync(businessId, cancellationToken);
            return Page();
        }

        Profile = result.Business!;
        SetInputFromProfile(Profile);
        StatusMessage = "Negocio actualizado.";

        _logger.LogInformation(
            "Admin {AdminUserId} updated business {BusinessId} profile with pilot enabled {IsPilotEnabled}.",
            AdminAuth.GetAdminUserId(User),
            Profile.BusinessId,
            Profile.IsPilotEnabled);

        return Page();
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(Guid businessId, CancellationToken cancellationToken)
    {
        if (!string.Equals(PasswordInput.NewPassword, PasswordInput.ConfirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "Las contrasenas no coinciden.");
            ClearPasswordFields();
            await LoadAsync(businessId, cancellationToken);
            return Page();
        }

        var result = await _adminApp.ResetBusinessPasswordAsync(
            new ResetBusinessPasswordCommand(
                businessId,
                AdminAuth.GetAdminUserId(User),
                PasswordInput.NewPassword),
            cancellationToken);

        ClearPasswordFields();

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo actualizar la contrasena.");
            await LoadAsync(businessId, cancellationToken);
            return Page();
        }

        Profile = result.Business!;
        SetInputFromProfile(Profile);
        StatusMessage = "Contrasena de negocio actualizada.";

        _logger.LogInformation(
            "Admin {AdminUserId} reset password for business {BusinessId}.",
            AdminAuth.GetAdminUserId(User),
            Profile.BusinessId);

        return Page();
    }

    public async Task<IActionResult> OnPostBrandingAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var logoPath = BrandingInput.LogoPath;
        if (BrandingInput.LogoUpload is { Length: > 0 })
        {
            var upload = await _logoUploads.SaveAsync(businessId, BrandingInput.LogoUpload, cancellationToken);
            if (!upload.Succeeded)
            {
                ModelState.AddModelError(string.Empty, upload.ErrorMessage ?? "No se pudo subir el logo.");
                ClearPasswordFields();
                await LoadAsync(businessId, cancellationToken);
                return Page();
            }

            logoPath = upload.PublicPath!;
        }

        var result = await _adminApp.UpdateBusinessBrandingAsync(
            new UpdateBusinessBrandingCommand(
                businessId,
                AdminAuth.GetAdminUserId(User),
                BrandingInput.PublicName,
                logoPath,
                BrandingInput.PrimaryColor,
                BrandingInput.SecondaryColor,
                BrandingInput.ProgramName,
                BrandingInput.ProgramDescription),
            cancellationToken);

        ClearPasswordFields();

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo actualizar el branding.");
            await LoadAsync(businessId, cancellationToken);
            return Page();
        }

        Profile = result.Business!;
        SetInputFromProfile(Profile);
        StatusMessage = "Branding del negocio actualizado.";

        _logger.LogInformation(
            "Admin {AdminUserId} updated branding for business {BusinessId}.",
            AdminAuth.GetAdminUserId(User),
            Profile.BusinessId);

        return Page();
    }

    public async Task<IActionResult> OnPostGenerateEnrollmentLinkAsync(
        Guid businessId,
        CancellationToken cancellationToken)
    {
        if (!await LoadAsync(businessId, cancellationToken))
        {
            return NotFound();
        }

        var token = await _businessEnrollmentLinks.CreateOrReplaceTokenAsync(businessId, cancellationToken);
        GeneratedEnrollmentUrl = $"{GetBaseUrl()}/Enroll/{token}";
        StatusMessage = "Link publico de registro generado. Solo se muestra en esta respuesta.";

        _logger.LogInformation(
            "Admin {AdminUserId} generated public enrollment link for business {BusinessId}.",
            AdminAuth.GetAdminUserId(User),
            businessId);

        await _adminApp.RecordBusinessEnrollmentLinkGeneratedAsync(
            new RecordBusinessEnrollmentLinkAuditCommand(AdminAuth.GetAdminUserId(User), businessId),
            cancellationToken);

        return Page();
    }

    private async Task<bool> LoadAsync(Guid businessId, CancellationToken cancellationToken)
    {
        Profile = await _adminApp.GetBusinessProfileAsync(businessId, cancellationToken);
        if (Profile is null)
        {
            return false;
        }

        SetInputFromProfile(Profile);
        ClearPasswordFields();
        return true;
    }

    private void SetInputFromProfile(BusinessProfileDto profile)
    {
        Input = new ProfileInputModel
        {
            BusinessName = profile.BusinessName,
            BusinessEmail = profile.BusinessEmail,
            BusinessLogo = profile.BusinessLogo,
            IsPilotEnabled = profile.IsPilotEnabled,
            ActivationStatus = profile.ActivationStatus,
            Notes = profile.Notes
        };

        BrandingInput = new BrandingInputModel
        {
            PublicName = profile.Branding.PublicName,
            LogoPath = profile.Branding.LogoPath,
            PrimaryColor = profile.Branding.PrimaryColor,
            SecondaryColor = profile.Branding.SecondaryColor,
            ProgramName = profile.Branding.ProgramName,
            ProgramDescription = profile.Branding.ProgramDescription
        };
    }

    private void ClearPasswordFields()
    {
        PasswordInput.NewPassword = string.Empty;
        PasswordInput.ConfirmPassword = string.Empty;
    }

    private string GetBaseUrl()
    {
        return EnrollmentBaseUrlResolver.Resolve(
            _configuration["DigitalCards:PublicBaseUrl"],
            Request.Scheme,
            Request.Host);
    }

    public sealed class ProfileInputModel
    {
        public string BusinessName { get; set; } = string.Empty;

        public string BusinessEmail { get; set; } = string.Empty;

        public string BusinessLogo { get; set; } = string.Empty;

        public bool IsPilotEnabled { get; set; }

        public BusinessActivationStatus? ActivationStatus { get; set; }

        public string? Notes { get; set; }
    }

    public sealed class PasswordInputModel
    {
        public string NewPassword { get; set; } = string.Empty;

        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public sealed class BrandingInputModel
    {
        public string PublicName { get; set; } = string.Empty;

        public string LogoPath { get; set; } = string.Empty;

        public IFormFile? LogoUpload { get; set; }

        public string PrimaryColor { get; set; } = string.Empty;

        public string SecondaryColor { get; set; } = string.Empty;

        public string ProgramName { get; set; } = string.Empty;

        public string ProgramDescription { get; set; } = string.Empty;
    }
}
