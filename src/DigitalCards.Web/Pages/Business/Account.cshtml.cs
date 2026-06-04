using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Domain;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Business;

[Authorize(Policy = BusinessAuth.Policy)]
public sealed class AccountModel : PageModel
{
    private readonly DigitalCardsAppService _appService;
    private readonly IBusinessSubscriptionRepository _subscriptions;
    private readonly IStripeService _stripe;
    private readonly BusinessSignupService _signupService;
    private readonly ILogger<AccountModel> _logger;

    public AccountModel(
        DigitalCardsAppService appService,
        IBusinessSubscriptionRepository subscriptions,
        IStripeService stripe,
        BusinessSignupService signupService,
        ILogger<AccountModel> logger)
    {
        _appService = appService;
        _subscriptions = subscriptions;
        _stripe = stripe;
        _signupService = signupService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? BusinessName { get; private set; }

    public string? StatusMessage { get; private set; }

    public BusinessSubscription? Subscription { get; private set; }

    [TempData]
    public string? SubscriptionStatusMessage { get; set; }

    [TempData]
    public string? SubscriptionErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var businessId = BusinessAuth.GetBusinessId(User);
        var settings = await _appService.GetBusinessBrandingSettingsAsync(businessId, cancellationToken);
        if (settings is null)
        {
            return RedirectToPage("/Business/Logout");
        }

        BusinessName = settings.BusinessName;
        ViewData["BusinessShellName"] = settings.Branding.PublicName;
        Subscription = await _subscriptions.FindByBusinessIdAsync(businessId, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var businessId = BusinessAuth.GetBusinessId(User);
        var settings = await _appService.GetBusinessBrandingSettingsAsync(businessId, cancellationToken);
        if (settings is null)
        {
            return RedirectToPage("/Business/Logout");
        }

        BusinessName = settings.BusinessName;
        ViewData["BusinessShellName"] = settings.Branding.PublicName;
        Subscription = await _subscriptions.FindByBusinessIdAsync(businessId, cancellationToken);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _appService.ChangeBusinessPasswordAsync(
            new ChangeBusinessPasswordCommand(
                businessId,
                Input.CurrentPassword,
                Input.NewPassword),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo cambiar la contrasena.");
            return Page();
        }

        Input = new InputModel();
        StatusMessage = "Contrasena actualizada correctamente. Se envio un correo de confirmacion.";
        return Page();
    }

    public async Task<IActionResult> OnPostOpenPortalAsync(CancellationToken cancellationToken)
    {
        var sub = await _subscriptions.FindByBusinessIdAsync(
            BusinessAuth.GetBusinessId(User),
            cancellationToken);

        if (sub is null ||
            string.IsNullOrEmpty(sub.StripeCustomerId) ||
            sub.SubscriptionStatus == "manual")
        {
            SubscriptionErrorMessage = "Tu plan no se administra desde Stripe.";
            return RedirectToPage();
        }

        try
        {
            var returnUrl = $"{Request.Scheme}://{Request.Host}/Business/Account";
            var portalUrl = await _stripe.CreatePortalSessionAsync(
                sub.StripeCustomerId,
                returnUrl,
                cancellationToken);
            return Redirect(portalUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create billing portal session for business {Id}.", sub.BusinessId);
            SubscriptionErrorMessage = "No se pudo abrir el portal de pagos. Intenta de nuevo.";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostSyncAsync(CancellationToken cancellationToken)
    {
        var businessId = BusinessAuth.GetBusinessId(User);
        try
        {
            var synced = await _signupService.SyncSubscriptionFromStripeAsync(businessId, cancellationToken);
            SubscriptionStatusMessage = synced
                ? "Suscripcion sincronizada con Stripe."
                : "No hay nada que sincronizar.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync subscription for business {Id}.", businessId);
            SubscriptionErrorMessage = "No se pudo sincronizar con Stripe. Intenta de nuevo.";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelAsync(CancellationToken cancellationToken)
    {
        var sub = await _subscriptions.FindByBusinessIdAsync(
            BusinessAuth.GetBusinessId(User),
            cancellationToken);

        if (sub is null ||
            string.IsNullOrEmpty(sub.StripeSubscriptionId) ||
            sub.SubscriptionStatus == "manual" ||
            sub.SubscriptionStatus == "canceled")
        {
            SubscriptionErrorMessage = "No hay suscripcion activa que cancelar.";
            return RedirectToPage();
        }

        try
        {
            await _stripe.CancelSubscriptionImmediatelyAsync(sub.StripeSubscriptionId, cancellationToken);
            SubscriptionStatusMessage = "Tu suscripcion fue cancelada.";
            _logger.LogInformation("Business {Id} requested immediate cancellation.", sub.BusinessId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel subscription for business {Id}.", sub.BusinessId);
            SubscriptionErrorMessage = "No se pudo cancelar la suscripcion. Intenta de nuevo.";
        }

        return RedirectToPage();
    }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "La contrasena actual es requerida.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contrasena nueva es requerida.")]
        [MinLength(8, ErrorMessage = "La contrasena nueva debe tener al menos 8 caracteres.")]
        [MaxLength(128, ErrorMessage = "La contrasena nueva no puede exceder 128 caracteres.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirma la contrasena nueva.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Las contrasenas no coinciden.")]
        [DataType(DataType.Password)]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
