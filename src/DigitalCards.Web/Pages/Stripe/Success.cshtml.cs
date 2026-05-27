using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Stripe;

[AllowAnonymous]
public sealed class SuccessModel : PageModel
{
    private readonly IBusinessSubscriptionRepository _subscriptions;
    private readonly IBusinessRepository _businesses;

    public SuccessModel(IBusinessSubscriptionRepository subscriptions, IBusinessRepository businesses)
    {
        _subscriptions = subscriptions;
        _businesses = businesses;
    }

    public bool IsActive { get; private set; }

    public async Task<IActionResult> OnGetAsync([FromQuery(Name = "session_id")] string? sessionId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return RedirectToPage("/Index");
        }

        var subscription = await _subscriptions.FindByCheckoutSessionIdAsync(sessionId, cancellationToken);
        if (subscription is null)
        {
            return RedirectToPage("/Index");
        }

        IsActive = subscription.SubscriptionStatus == "active";

        if (IsActive)
        {
            var business = await _businesses.FindByIdAsync(subscription.BusinessId, cancellationToken);
            if (business is not null && !User.Identity!.IsAuthenticated)
            {
                var principal = BusinessAuth.CreatePrincipal(new BusinessDto(business.Id, business.Name, business.Email, business.LogoPath));
                var properties = new AuthenticationProperties { IsPersistent = true };
                await HttpContext.SignInAsync(BusinessAuth.Scheme, principal, properties);
            }
        }

        return Page();
    }
}
