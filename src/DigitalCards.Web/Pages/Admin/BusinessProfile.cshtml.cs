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
    private readonly DigitalCardsAppService _appService;
    private readonly IBusinessEnrollmentLinkService _businessEnrollmentLinks;
    private readonly BusinessLogoUploadService _logoUploads;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BusinessProfileModel> _logger;

    public BusinessProfileModel(
        AdminAppService adminApp,
        DigitalCardsAppService appService,
        IBusinessEnrollmentLinkService businessEnrollmentLinks,
        BusinessLogoUploadService logoUploads,
        IConfiguration configuration,
        ILogger<BusinessProfileModel> logger)
    {
        _adminApp = adminApp;
        _appService = appService;
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

    [BindProperty]
    public int RefreshLimit { get; set; } = 25;

    public BusinessProfileDto? Profile { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? GeneratedEnrollmentUrl { get; private set; }

    public WalletBrandingRefreshResult? RefreshResult { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid businessId, CancellationToken cancellationToken)
    {
        return await LoadAsync(businessId, cancellationToken)
            ? Page()
            : NotFound();
    }

    public async Task<IActionResult> OnPostSaveAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var activationStatus = Input.ActivationStatus == BusinessActivationStatus.Inactive
            ? BusinessActivationStatus.Inactive
            : BusinessActivationStatus.ModernPrimary;
        var isActive = activationStatus != BusinessActivationStatus.Inactive;
        var result = await _adminApp.UpdateBusinessProfileAsync(
            new UpdateBusinessProfileCommand(
                businessId,
                AdminAuth.GetAdminUserId(User),
                Input.BusinessName,
                Input.BusinessEmail,
                Input.BusinessLogo,
                isActive,
                Notes: null,
                activationStatus),
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

    public async Task<IActionResult> OnPostSendInviteAsync(Guid businessId, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(businessId, cancellationToken))
        {
            return NotFound();
        }

        await _appService.RequestBusinessPasswordResetAsync(
            new RequestBusinessPasswordResetCommand(Profile!.BusinessEmail, GetBaseUrl()),
            cancellationToken);

        StatusMessage = "Invitacion enviada por correo para que el negocio configure su acceso.";

        _logger.LogInformation(
            "Admin {AdminUserId} sent onboarding invite for business {BusinessId}.",
            AdminAuth.GetAdminUserId(User),
            businessId);

        return Page();
    }

    public async Task<IActionResult> OnPostBrandingAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var previousProfile = await _adminApp.GetBusinessProfileAsync(businessId, cancellationToken);
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
        if (!string.IsNullOrWhiteSpace(logoPath) &&
            !string.Equals(previousProfile?.Branding.LogoPath, logoPath, StringComparison.OrdinalIgnoreCase))
        {
            _logoUploads.DeleteIfOwned(previousProfile?.Branding.LogoPath);
        }

        _logger.LogInformation(
            "Admin {AdminUserId} updated branding for business {BusinessId}.",
            AdminAuth.GetAdminUserId(User),
            Profile.BusinessId);

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteBusinessAsync(
        Guid businessId,
        string confirmation,
        CancellationToken cancellationToken)
    {
        var previousProfile = await _adminApp.GetBusinessProfileAsync(businessId, cancellationToken);
        var result = await _adminApp.DeleteBusinessPermanentlyAsync(
            new DeleteBusinessCommand(
                businessId,
                AdminAuth.GetAdminUserId(User),
                confirmation),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo eliminar el negocio.");
            await LoadAsync(businessId, cancellationToken);
            return Page();
        }

        _logoUploads.DeleteIfOwned(previousProfile?.Branding.LogoPath);

        _logger.LogWarning(
            "Admin {AdminUserId} permanently deleted business {BusinessId}.",
            AdminAuth.GetAdminUserId(User),
            businessId);

        TempData["AdminBusinessStatus"] = "Negocio eliminado permanentemente.";
        return RedirectToPage("/Admin/Businesses");
    }

    public async Task<IActionResult> OnPostRefreshWalletBrandingAsync(Guid businessId, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(businessId, cancellationToken))
        {
            return NotFound();
        }

        var result = await _adminApp.RefreshBusinessWalletBrandingAsync(
            new AdminWalletBrandingRefreshCommand(
                businessId,
                AdminAuth.GetAdminUserId(User),
                RefreshLimit),
            cancellationToken);

        if (!result.Succeeded || result.Refresh is null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo refrescar el branding Wallet.");
            return Page();
        }

        RefreshResult = result.Refresh;
        StatusMessage = ToRefreshStatus(RefreshResult);

        _logger.LogInformation(
            "Admin {AdminUserId} refreshed wallet branding for business {BusinessId}.",
            AdminAuth.GetAdminUserId(User),
            businessId);

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
            ActivationStatus = !profile.IsPilotEnabled || profile.ActivationStatus == BusinessActivationStatus.Inactive
                ? BusinessActivationStatus.Inactive
                : BusinessActivationStatus.ModernPrimary
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

    private static string ToRefreshStatus(WalletBrandingRefreshResult result)
    {
        var attempted = result.GoogleWalletAttempted + result.AppleWalletAttempted;
        var succeeded = result.GoogleWalletSucceeded + result.AppleWalletSucceeded;
        var status = $"Refresh Wallet ejecutado: {result.CardsWithTrackedWallets} tarjetas con Wallet, {succeeded}/{attempted} actualizaciones completadas.";
        return result.HasWarnings
            ? $"{status} Hubo alertas seguras: {string.Join(", ", result.SafeErrors)}."
            : status;
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
