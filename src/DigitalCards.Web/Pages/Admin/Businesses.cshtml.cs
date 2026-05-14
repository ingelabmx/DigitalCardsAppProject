using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Admin;

[Authorize(Policy = AdminAuth.Policy)]
public sealed class BusinessesModel : PageModel
{
    private readonly AdminAppService _adminApp;
    private readonly ILogger<BusinessesModel> _logger;

    public BusinessesModel(AdminAppService adminApp, ILogger<BusinessesModel> logger)
    {
        _adminApp = adminApp;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    public IReadOnlyList<PilotBusinessDto> Businesses { get; private set; } = [];

    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        StatusMessage = TempData["AdminBusinessStatus"] as string;
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostEnableAsync(
        Guid businessId,
        string? notes,
        CancellationToken cancellationToken)
    {
        return await SetPilotAsync(businessId, notes, isEnabled: true, cancellationToken);
    }

    public async Task<IActionResult> OnPostDisableAsync(
        Guid businessId,
        string? notes,
        CancellationToken cancellationToken)
    {
        return await SetPilotAsync(businessId, notes, isEnabled: false, cancellationToken);
    }

    private async Task<IActionResult> SetPilotAsync(
        Guid businessId,
        string? notes,
        bool isEnabled,
        CancellationToken cancellationToken)
    {
        if (businessId == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "El negocio no existe.");
        }

        if (notes?.Length > 500)
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
                businessId,
                AdminAuth.GetAdminUserId(User),
                isEnabled,
                notes,
                isEnabled ? DigitalCards.Domain.BusinessActivationStatus.ModernPrimary : DigitalCards.Domain.BusinessActivationStatus.Inactive),
            cancellationToken);

        if (result is null)
        {
            ModelState.AddModelError(string.Empty, "El negocio no existe.");
            await LoadAsync(cancellationToken);
            return Page();
        }

        _logger.LogInformation(
            "Admin {AdminUserId} changed pilot business {BusinessId} enabled {IsEnabled}.",
            AdminAuth.GetAdminUserId(User),
            result.BusinessId,
            result.IsEnabled);
        StatusMessage = result.IsEnabled
            ? $"Negocio activado: {result.BusinessName}."
            : $"Negocio desactivado: {result.BusinessName}.";
        await LoadAsync(cancellationToken);
        return Page();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Businesses = await _adminApp.ListPilotBusinessesAsync(Query ?? string.Empty, cancellationToken);
    }
}
