using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Branding;
using DigitalCards.Web.Pilot;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Business;

[Authorize(Policy = BusinessAuth.Policy)]
public sealed class BrandingModel : PageModel
{
    private readonly DigitalCardsAppService _appService;
    private readonly BusinessLogoUploadService _logoUploads;
    private readonly PilotAccessService _pilotAccess;
    private readonly ILogger<BrandingModel> _logger;

    public BrandingModel(
        DigitalCardsAppService appService,
        BusinessLogoUploadService logoUploads,
        PilotAccessService pilotAccess,
        ILogger<BrandingModel> logger)
    {
        _appService = appService;
        _logoUploads = logoUploads;
        _pilotAccess = pilotAccess;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public int RefreshLimit { get; set; } = 25;

    public BusinessBrandingSettingsDto? Settings { get; private set; }

    public string? StatusMessage { get; private set; }

    public WalletBrandingRefreshResult? RefreshResult { get; private set; }

    public string? PilotBlockMessage { get; private set; }

    public bool IsPilotBlocked => PilotBlockMessage is not null;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        return await LoadAsync(cancellationToken, populateInput: true)
            ? Page()
            : RedirectToPage("/Business/Logout");
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!await LoadAsync(cancellationToken, populateInput: false))
        {
            return RedirectToPage("/Business/Logout");
        }

        if (IsPilotBlocked)
        {
            ModelState.AddModelError(string.Empty, PilotBlockMessage!);
            return Page();
        }

        var previousLogoPath = Settings!.Branding.LogoPath;
        var logoPath = Input.LogoPath;
        if (Input.LogoUpload is { Length: > 0 })
        {
            var upload = await _logoUploads.SaveAsync(BusinessAuth.GetBusinessId(User), Input.LogoUpload, cancellationToken);
            if (!upload.Succeeded)
            {
                ModelState.AddModelError(string.Empty, upload.ErrorMessage ?? "No se pudo subir el logo.");
                return Page();
            }

            logoPath = upload.PublicPath!;
        }

        var result = await _appService.UpdateBusinessBrandingAsync(
            new UpdateBusinessSelfServiceBrandingCommand(
                BusinessAuth.GetBusinessId(User),
                Input.PublicName,
                logoPath,
                Input.PrimaryColor,
                Input.SecondaryColor,
                Input.ProgramName,
                Input.ProgramDescription),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo actualizar el branding.");
            return Page();
        }

        Settings = result.Settings!;
        SetInputFromSettings(Settings);
        StatusMessage = "Branding actualizado.";
        if (!string.IsNullOrWhiteSpace(logoPath) &&
            !string.Equals(previousLogoPath, logoPath, StringComparison.OrdinalIgnoreCase))
        {
            _logoUploads.DeleteIfOwned(previousLogoPath);
        }

        _logger.LogInformation(
            "Business {BusinessId} updated self-service branding.",
            Settings.BusinessId);
        return Page();
    }

    public async Task<IActionResult> OnPostRefreshWalletsAsync(CancellationToken cancellationToken)
    {
        if (!await LoadAsync(cancellationToken, populateInput: true))
        {
            return RedirectToPage("/Business/Logout");
        }

        if (IsPilotBlocked)
        {
            ModelState.AddModelError(string.Empty, PilotBlockMessage!);
            return Page();
        }

        RefreshResult = await _appService.RefreshBusinessWalletBrandingAsync(
            new WalletBrandingRefreshCommand(BusinessAuth.GetBusinessId(User), RefreshLimit),
            cancellationToken);

        if (RefreshResult.ErrorMessage is not null)
        {
            ModelState.AddModelError(string.Empty, RefreshResult.ErrorMessage);
            return Page();
        }

        StatusMessage = ToRefreshStatus(RefreshResult);

        _logger.LogInformation(
            "Business {BusinessId} refreshed wallet branding.",
            Settings!.BusinessId);

        return Page();
    }

    private async Task<bool> LoadAsync(CancellationToken cancellationToken, bool populateInput)
    {
        var businessId = BusinessAuth.GetBusinessId(User);
        Settings = await _appService.GetBusinessBrandingSettingsAsync(businessId, cancellationToken);
        if (Settings is null)
        {
            return false;
        }

        var pilotAccess = await _pilotAccess.CheckAuthenticatedBusinessAsync(User, cancellationToken);
        PilotBlockMessage = pilotAccess.IsAllowed ? null : pilotAccess.Message;
        if (populateInput)
        {
            SetInputFromSettings(Settings);
        }

        return true;
    }

    private void SetInputFromSettings(BusinessBrandingSettingsDto settings)
    {
        Input = new InputModel
        {
            PublicName = settings.Branding.PublicName,
            LogoPath = settings.Branding.LogoPath,
            PrimaryColor = settings.Branding.PrimaryColor,
            SecondaryColor = settings.Branding.SecondaryColor,
            ProgramName = settings.Branding.ProgramName,
            ProgramDescription = settings.Branding.ProgramDescription
        };
    }

    private static string ToRefreshStatus(WalletBrandingRefreshResult result)
    {
        var status = $"Refresh Wallet ejecutado: {result.CardsWithTrackedWallets} tarjetas con Wallet, Google {result.GoogleWalletSucceeded}/{result.GoogleWalletAttempted}, Apple {result.AppleWalletSucceeded}/{result.AppleWalletAttempted}.";
        return result.HasWarnings
            ? $"{status} Hubo alertas seguras: {string.Join(", ", result.SafeErrors)}."
            : status;
    }

    public sealed class InputModel
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
