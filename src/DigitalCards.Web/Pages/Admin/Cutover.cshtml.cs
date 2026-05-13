using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Domain;
using DigitalCards.Infrastructure.LegacySync;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace DigitalCards.Web.Pages.Admin;

[Authorize(Policy = AdminAuth.Policy)]
public sealed class CutoverModel : PageModel
{
    private readonly AdminAppService _adminApp;
    private readonly LegacyWalletSyncOptions _legacyWalletSyncOptions;
    private readonly LegacyWalletSyncState _legacyWalletSyncState;
    private readonly ILogger<CutoverModel> _logger;

    public CutoverModel(
        AdminAppService adminApp,
        IOptions<LegacyWalletSyncOptions> legacyWalletSyncOptions,
        LegacyWalletSyncState legacyWalletSyncState,
        ILogger<CutoverModel> logger)
    {
        _adminApp = adminApp;
        _legacyWalletSyncOptions = legacyWalletSyncOptions.Value;
        _legacyWalletSyncState = legacyWalletSyncState;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    [BindProperty(SupportsGet = true)]
    public BusinessActivationStatus? ActivationStatusFilter { get; set; }

    [BindProperty]
    public CutoverStatusInput Input { get; set; } = new();

    public IReadOnlyList<CutoverBusinessViewModel> Businesses { get; private set; } = [];

    public string? StatusMessage { get; private set; }

    public LegacyWalletSyncStateSnapshot LegacyWalletSyncState =>
        _legacyWalletSyncState.Snapshot(_legacyWalletSyncOptions.Enabled);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostStatusAsync(CancellationToken cancellationToken)
    {
        if (Input.BusinessId == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "El negocio no existe.");
        }

        if (Input.Notes?.Length > 500)
        {
            ModelState.AddModelError(string.Empty, "Las notas no pueden exceder 500 caracteres.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        var result = await _adminApp.SetPilotBusinessAsync(
            new SetPilotBusinessCommand(
                Input.BusinessId,
                AdminAuth.GetAdminUserId(User),
                IsModernEnabled(Input.ActivationStatus),
                Input.Notes,
                Input.ActivationStatus),
            cancellationToken);

        if (result is null)
        {
            ModelState.AddModelError(string.Empty, "El negocio no existe.");
            await LoadAsync(cancellationToken);
            return Page();
        }

        _logger.LogInformation(
            "Admin {AdminUserId} changed cutover status for business {BusinessId} to {ActivationStatus}.",
            AdminAuth.GetAdminUserId(User),
            result.BusinessId,
            result.ActivationStatus);

        StatusMessage = $"{result.BusinessName} actualizado a {ActivationLabel(result.ActivationStatus)}.";
        await LoadAsync(cancellationToken);
        return Page();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var businesses = await _adminApp.ListPilotBusinessesAsync(Query ?? string.Empty, cancellationToken);
        var reports = await _adminApp.GetReportsAsync(cancellationToken);
        var reportsByBusinessId = reports.Businesses.ToDictionary(report => report.BusinessId);
        var views = new List<CutoverBusinessViewModel>(businesses.Count);

        foreach (var business in businesses)
        {
            if (ActivationStatusFilter is not null && business.ActivationStatus != ActivationStatusFilter)
            {
                continue;
            }

            reportsByBusinessId.TryGetValue(business.BusinessId, out var report);
            var profile = await _adminApp.GetBusinessProfileAsync(business.BusinessId, cancellationToken);
            views.Add(CutoverBusinessViewModel.Create(business, report, profile?.Branding));
        }

        Businesses = views
            .OrderBy(view => view.Business.ActivationStatus)
            .ThenBy(view => view.Business.BusinessName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsModernEnabled(BusinessActivationStatus activationStatus)
    {
        return activationStatus is not BusinessActivationStatus.LegacyOnly
            and not BusinessActivationStatus.Inactive;
    }

    public static string ActivationLabel(BusinessActivationStatus status)
    {
        return status switch
        {
            BusinessActivationStatus.PilotModern => "Piloto moderno",
            BusinessActivationStatus.ModernPrimary => "Moderno principal",
            BusinessActivationStatus.LegacyRetired => "Legacy retirado",
            BusinessActivationStatus.Inactive => "Inactivo",
            _ => "Legacy only"
        };
    }
}

public sealed class CutoverStatusInput
{
    public Guid BusinessId { get; set; }

    public BusinessActivationStatus ActivationStatus { get; set; }

    public string? Notes { get; set; }
}

public sealed record CutoverBusinessViewModel(
    PilotBusinessDto Business,
    AdminReportBusinessDto? Report,
    bool HasBranding,
    int ReadinessScore,
    IReadOnlyList<string> ReadySignals,
    IReadOnlyList<string> MissingSignals)
{
    public static CutoverBusinessViewModel Create(
        PilotBusinessDto business,
        AdminReportBusinessDto? report,
        BusinessBrandingDto? branding)
    {
        var ready = new List<string>();
        var missing = new List<string>();
        var hasBranding = branding?.UpdatedAt is not null;
        AddSignal(hasBranding, "Branding configurado", "Branding pendiente", ready, missing);
        AddSignal(report?.CardCount > 0, "Tarjetas asociadas", "Sin tarjetas asociadas", ready, missing);
        AddSignal(report?.LastStampedAt is not null, "Sellos recientes", "Sin sellos recientes", ready, missing);
        AddSignal((report?.GoogleIssuedCount ?? 0) > 0, "Google Wallet emitida", "Google Wallet pendiente", ready, missing);
        AddSignal((report?.AppleTrackedCount ?? 0) > 0, "Apple Wallet tracked", "Apple Wallet pendiente", ready, missing);
        AddSignal((report?.WalletIssueCount ?? 0) == 0, "Sin errores Wallet", "Errores Wallet recientes", ready, missing);

        return new CutoverBusinessViewModel(
            business,
            report,
            hasBranding,
            ready.Count,
            ready,
            missing);
    }

    private static void AddSignal(
        bool isReady,
        string readyLabel,
        string missingLabel,
        ICollection<string> ready,
        ICollection<string> missing)
    {
        if (isReady)
        {
            ready.Add(readyLabel);
        }
        else
        {
            missing.Add(missingLabel);
        }
    }
}
