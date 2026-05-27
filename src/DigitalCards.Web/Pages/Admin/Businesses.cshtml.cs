using DigitalCards.Application.Abstractions;
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
    private readonly BusinessSignupService _signupService;
    private readonly IBusinessSubscriptionRepository _subscriptions;
    private readonly ILogger<BusinessesModel> _logger;

    public BusinessesModel(
        AdminAppService adminApp,
        BusinessSignupService signupService,
        IBusinessSubscriptionRepository subscriptions,
        ILogger<BusinessesModel> logger)
    {
        _adminApp = adminApp;
        _signupService = signupService;
        _subscriptions = subscriptions;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 10;

    [BindProperty(SupportsGet = true)]
    public string? SubscriptionFilter { get; set; }

    public IReadOnlyList<PilotBusinessDto> Businesses { get; private set; } = [];

    public IReadOnlyList<PilotBusinessDto> PagedBusinesses { get; private set; } = [];

    public IReadOnlyList<AbandonedSignupDto> AbandonedSignups { get; private set; } = [];

    public string? StatusMessage { get; private set; }

    public int TotalPages { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        StatusMessage = TempData["AdminBusinessStatus"] as string;
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostEnableAsync(
        Guid businessId,
        CancellationToken cancellationToken)
    {
        return await SetPilotAsync(businessId, isEnabled: true, cancellationToken);
    }

    public async Task<IActionResult> OnPostDisableAsync(
        Guid businessId,
        CancellationToken cancellationToken)
    {
        return await SetPilotAsync(businessId, isEnabled: false, cancellationToken);
    }

    public async Task<IActionResult> OnPostResendPaymentLinkAsync(
        Guid businessId,
        CancellationToken cancellationToken)
    {
        if (businessId == Guid.Empty)
        {
            TempData["AdminBusinessStatus"] = "ID de negocio invalido.";
            return RedirectToPage();
        }

        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var checkoutUrl = await _signupService.CreateOrResumeCheckoutAsync(businessId, baseUrl, cancellationToken);
            TempData["AdminBusinessStatus"] = $"Link de pago generado: {checkoutUrl}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating checkout link for business {BusinessId}.", businessId);
            TempData["AdminBusinessStatus"] = "Error al generar el link de pago.";
        }

        return RedirectToPage();
    }

    private async Task<IActionResult> SetPilotAsync(
        Guid businessId,
        bool isEnabled,
        CancellationToken cancellationToken)
    {
        if (businessId == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "El negocio no existe.");
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
                Notes: null,
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
        var all = await _adminApp.ListPilotBusinessesAsync(Query ?? string.Empty, cancellationToken);

        var enriched = new List<PilotBusinessDto>(all.Count);
        foreach (var b in all)
        {
            var sub = await _subscriptions.FindByBusinessIdAsync(b.BusinessId, cancellationToken);
            enriched.Add(b with
            {
                SubscriptionStatus = sub?.SubscriptionStatus,
                StripePlanKey = sub?.StripePlanKey,
                GraceEndsAt = sub?.GraceEndsAt
            });
        }

        Businesses = SubscriptionFilter switch
        {
            "active" => enriched.Where(b => b.SubscriptionStatus == "active").ToArray(),
            "past_due" => enriched.Where(b => b.SubscriptionStatus is "past_due" or "canceled").ToArray(),
            "none" => enriched.Where(b => b.SubscriptionStatus is null).ToArray(),
            _ => enriched
        };

        PageSize = NormalizePageSize(PageSize);
        TotalPages = Math.Max(1, (int)Math.Ceiling(Businesses.Count / (double)PageSize));
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);
        PagedBusinesses = Businesses
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToArray();
        AbandonedSignups = await _signupService.ListAbandonedAsync(cancellationToken);
    }

    private static int NormalizePageSize(int value)
    {
        return value is 20 or 50 ? value : 10;
    }
}
