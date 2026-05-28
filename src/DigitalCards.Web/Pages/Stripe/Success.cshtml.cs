using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Domain;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Stripe;

[AllowAnonymous]
public sealed class SuccessModel : PageModel
{
    private readonly IStripeService _stripe;
    private readonly IBusinessRepository _businesses;
    private readonly IBusinessSubscriptionRepository _subscriptions;
    private readonly ILogger<SuccessModel> _logger;

    public SuccessModel(
        IStripeService stripe,
        IBusinessRepository businesses,
        IBusinessSubscriptionRepository subscriptions,
        ILogger<SuccessModel> logger)
    {
        _stripe = stripe;
        _businesses = businesses;
        _subscriptions = subscriptions;
        _logger = logger;
    }

    public bool IsActive { get; private set; }

    public async Task<IActionResult> OnGetAsync(
        [FromQuery(Name = "session_id")] string? sessionId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return RedirectToPage("/Index");

        // 1. Obtener businessId desde la sesión de Stripe
        string? businessIdStr;
        try
        {
            (businessIdStr, _) = await _stripe.GetCheckoutSessionMetadataAsync(sessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not retrieve Stripe session {SessionId}: {Message}", sessionId, ex.Message);
            return RedirectToPage("/Index");
        }

        if (!Guid.TryParse(businessIdStr, out var businessId))
            return RedirectToPage("/Index");

        // 2. Cargar negocio
        var business = await _businesses.FindByIdAsync(businessId, cancellationToken);
        if (business is null)
            return RedirectToPage("/Index");

        // 3. Reintentos: esperar hasta 3s a que el webhook active el negocio
        BusinessSubscription? subscription = null;
        for (int i = 0; i < 3; i++)
        {
            subscription = await _subscriptions.FindByBusinessIdAsync(businessId, cancellationToken);
            if (subscription?.SubscriptionStatus == "active") break;
            await Task.Delay(1000, cancellationToken);
        }

        IsActive = subscription?.SubscriptionStatus == "active";

        // 4. Si activo: login automático + redirect al Dashboard
        if (IsActive && !User.Identity!.IsAuthenticated)
        {
            var principal = BusinessAuth.CreatePrincipal(
                new BusinessDto(business.Id, business.Name, business.Email, business.LogoPath));
            var properties = new AuthenticationProperties
            {
                IsPersistent = true,
                IssuedUtc = DateTimeOffset.UtcNow
            };
            await HttpContext.SignInAsync(BusinessAuth.Scheme, principal, properties);
            return RedirectToPage("/Business/Dashboard");
        }

        // 5. Webhook no llegó aún — mostrar página con auto-refresh
        return Page();
    }
}
