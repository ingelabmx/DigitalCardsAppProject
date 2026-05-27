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
    private readonly IBusinessSubscriptionRepository _subscriptions;
    private readonly BusinessLogoUploadService _logoUploads;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BusinessProfileModel> _logger;

    public BusinessProfileModel(
        AdminAppService adminApp,
        DigitalCardsAppService appService,
        IBusinessEnrollmentLinkService businessEnrollmentLinks,
        IBusinessSubscriptionRepository subscriptions,
        BusinessLogoUploadService logoUploads,
        IConfiguration configuration,
        ILogger<BusinessProfileModel> logger)
    {
        _adminApp = adminApp;
        _appService = appService;
        _businessEnrollmentLinks = businessEnrollmentLinks;
        _subscriptions = subscriptions;
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

    public WalletBrandingRefreshResult? RefreshResult { get; private set; }

    public bool BrandingLogoUnavailable { get; private set; }

    public CutoverBusinessViewModel? OperationalView { get; private set; }

    public BusinessSubscription? Subscription { get; private set; }

    [BindProperty]
    public BusinessProfileSmokeInput SmokeInput { get; set; } = new();

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
        return await SaveBrandingAsync(businessId, refreshAfterSave: false, cancellationToken);
    }

    public async Task<IActionResult> OnPostSaveAndRefreshAsync(Guid businessId, CancellationToken cancellationToken)
    {
        return await SaveBrandingAsync(businessId, refreshAfterSave: true, cancellationToken);
    }

    private async Task<IActionResult> SaveBrandingAsync(
        Guid businessId,
        bool refreshAfterSave,
        CancellationToken cancellationToken)
    {
        var previousProfile = await _adminApp.GetBusinessProfileAsync(businessId, cancellationToken);
        var previousLogoPath = previousProfile?.Branding.LogoPath;
        var logoPath = previousLogoPath ?? string.Empty;
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
        else if (IsManagedLogoMissing(previousLogoPath))
        {
            ModelState.AddModelError(string.Empty, "El logo actual no esta disponible; sube un PNG nuevo.");
            ClearPasswordFields();
            await LoadAsync(businessId, cancellationToken);
            return Page();
        }

        var result = await _adminApp.UpdateBusinessBrandingAsync(
            new UpdateBusinessBrandingCommand(
                businessId,
                AdminAuth.GetAdminUserId(User),
                BrandingInput.PublicName,
                logoPath,
                BrandingInput.PrimaryColor,
                BrandingInput.SecondaryColor,
                BrandingInput.CustomFieldColor,
                BrandingInput.StampGoal,
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
            !string.Equals(previousLogoPath, logoPath, StringComparison.OrdinalIgnoreCase))
        {
            _logoUploads.DeleteIfOwned(previousLogoPath);
        }

        _logger.LogInformation(
            "Admin {AdminUserId} updated branding for business {BusinessId}.",
            AdminAuth.GetAdminUserId(User),
            Profile.BusinessId);

        if (!refreshAfterSave)
        {
            return Page();
        }

        if (await RefreshWalletBrandingAsync(businessId, cancellationToken))
        {
            StatusMessage = $"Branding del negocio actualizado. {ToRefreshStatus(RefreshResult!)}";
        }

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

        if (await RefreshWalletBrandingAsync(businessId, cancellationToken))
        {
            StatusMessage = ToRefreshStatus(RefreshResult!);
        }

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

        var token = await _businessEnrollmentLinks.GetOrCreateTokenAsync(businessId, cancellationToken);
        GeneratedEnrollmentUrl = $"{GetBaseUrl()}/Enroll/{token}";
        StatusMessage = "Link publico de registro. Siempre es el mismo para este negocio.";

        _logger.LogInformation(
            "Admin {AdminUserId} generated public enrollment link for business {BusinessId}.",
            AdminAuth.GetAdminUserId(User),
            businessId);

        await _adminApp.RecordBusinessEnrollmentLinkGeneratedAsync(
            new RecordBusinessEnrollmentLinkAuditCommand(AdminAuth.GetAdminUserId(User), businessId),
            cancellationToken);

        return Page();
    }

    public async Task<IActionResult> OnPostSmokeAsync(Guid businessId, CancellationToken cancellationToken)
    {
        if (SmokeInput.Notes?.Length > 500)
        {
            ModelState.AddModelError(string.Empty, "Las notas no pueden exceder 500 caracteres.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(businessId, cancellationToken);
            return Page();
        }

        var result = await _adminApp.RecordCutoverSmokeEvidenceAsync(
            new RecordCutoverSmokeEvidenceCommand(
                businessId,
                AdminAuth.GetAdminUserId(User),
                SmokeInput.HealthOk,
                SmokeInput.ReadyOk,
                SmokeInput.EmailOk,
                SmokeInput.WalletMobileOk,
                SmokeInput.WalletSavedOk,
                SmokeInput.ModernStampOk,
                SmokeInput.SupportReviewed,
                SmokeInput.Notes),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo guardar la evidencia.");
            await LoadAsync(businessId, cancellationToken);
            return Page();
        }

        StatusMessage = result.Evidence!.IsComplete
            ? "Smoke de activacion registrado como completo."
            : "Smoke de activacion registrado con pendientes.";

        _logger.LogInformation(
            "Admin {AdminUserId} recorded activation smoke evidence for business {BusinessId}.",
            AdminAuth.GetAdminUserId(User),
            businessId);

        await LoadAsync(businessId, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostUpdatePlanAsync(Guid businessId, string planKey, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(businessId, cancellationToken))
        {
            return NotFound();
        }

        var maxClients = planKey switch
        {
            "Basic"    => 300,
            "Pro"      => 1000,
            "Business" => -1,
            _          => -1
        };

        var now = DateTimeOffset.UtcNow;
        if (Subscription is not null)
        {
            await _subscriptions.UpsertAsync(
                Subscription.WithPlanUpdate(planKey, maxClients, now),
                cancellationToken);
        }
        else
        {
            await _subscriptions.UpsertAsync(
                new BusinessSubscription(
                    businessId,
                    subscriptionStatus: "manual",
                    maxClients: maxClients,
                    createdViaSelfService: false,
                    createdAt: now,
                    updatedAt: now,
                    stripePlanKey: planKey),
                cancellationToken);
        }

        Subscription = await _subscriptions.FindByBusinessIdAsync(businessId, cancellationToken);

        var label = planKey switch
        {
            "Basic"    => "Basico",
            "Pro"      => "Pro",
            "Business" => "Empresarial",
            _          => "Manual"
        };
        StatusMessage = $"Plan actualizado a {label}.";

        _logger.LogInformation(
            "Admin {AdminUserId} updated plan for business {BusinessId} to {PlanKey}.",
            AdminAuth.GetAdminUserId(User),
            businessId,
            planKey);

        return Page();
    }

    private async Task<bool> LoadAsync(Guid businessId, CancellationToken cancellationToken)
    {
        Profile = await _adminApp.GetBusinessProfileAsync(businessId, cancellationToken);
        if (Profile is null)
        {
            return false;
        }

        Subscription = await _subscriptions.FindByBusinessIdAsync(businessId, cancellationToken);
        SetInputFromProfile(Profile);
        var reports = await _adminApp.GetReportsAsync(cancellationToken);
        var report = reports.Businesses.SingleOrDefault(item => item.BusinessId == businessId);
        var smokeEvidence = await _adminApp.ListCutoverSmokeEvidenceAsync(businessId, limit: 1, cancellationToken);
        OperationalView = CutoverBusinessViewModel.Create(
            new PilotBusinessDto(
                Profile.BusinessId,
                Profile.BusinessName,
                Profile.BusinessEmail,
                Profile.IsPilotEnabled,
                Profile.ActivationStatus,
                report?.ClientCount ?? 0,
                report?.CurrentStampTotal ?? 0,
                Notes: null,
                UpdatedAt: Profile.PilotUpdatedAt),
            report,
            Profile.Branding,
            smokeEvidence.FirstOrDefault());
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
            PrimaryColor = profile.Branding.PrimaryColor,
            SecondaryColor = profile.Branding.SecondaryColor,
            CustomFieldColor = profile.Branding.CustomFieldColor,
            StampGoal = profile.Branding.StampGoal,
            ProgramName = profile.Branding.ProgramName,
            ProgramDescription = profile.Branding.ProgramDescription
        };
        BrandingLogoUnavailable = IsManagedLogoMissing(profile.Branding.LogoPath);
    }

    private bool IsManagedLogoMissing(string? logoPath)
    {
        return _logoUploads.IsOwned(logoPath) &&
            !_logoUploads.ExistsIfOwned(logoPath);
    }

    private void ClearPasswordFields()
    {
        PasswordInput.NewPassword = string.Empty;
        PasswordInput.ConfirmPassword = string.Empty;
    }

    private async Task<bool> RefreshWalletBrandingAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var result = await _adminApp.RefreshBusinessWalletBrandingAsync(
            new AdminWalletBrandingRefreshCommand(
                businessId,
                AdminAuth.GetAdminUserId(User),
                Limit: 0),
            cancellationToken);

        if (!result.Succeeded || result.Refresh is null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo actualizar tarjetas.");
            return false;
        }

        RefreshResult = result.Refresh;

        _logger.LogInformation(
            "Admin {AdminUserId} refreshed wallet branding for business {BusinessId}.",
            AdminAuth.GetAdminUserId(User),
            businessId);

        return true;
    }

    private static string ToRefreshStatus(WalletBrandingRefreshResult result)
    {
        var attempted = result.GoogleWalletAttempted + result.AppleWalletAttempted;
        var succeeded = result.GoogleWalletSucceeded + result.AppleWalletSucceeded;
        var status = $"Actualizacion ejecutada: {result.CardsWithTrackedWallets} tarjetas digitales, {succeeded}/{attempted} actualizaciones completadas.";
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

        public IFormFile? LogoUpload { get; set; }

        public string PrimaryColor { get; set; } = string.Empty;

        public string SecondaryColor { get; set; } = string.Empty;

        public string CustomFieldColor { get; set; } = string.Empty;

        public int StampGoal { get; set; } = 10;

        public string ProgramName { get; set; } = string.Empty;

        public string ProgramDescription { get; set; } = string.Empty;
    }

    public sealed class BusinessProfileSmokeInput
    {
        public bool HealthOk { get; set; }

        public bool ReadyOk { get; set; }

        public bool EmailOk { get; set; }

        public bool WalletMobileOk { get; set; }

        public bool WalletSavedOk { get; set; }

        public bool ModernStampOk { get; set; }

        public bool SupportReviewed { get; set; }

        public string? Notes { get; set; }
    }
}
