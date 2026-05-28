using DigitalCards.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Stripe;

[AllowAnonymous]
public sealed class CancelModel : PageModel
{
    private readonly BusinessSignupService _signupService;

    public CancelModel(BusinessSignupService signupService)
    {
        _signupService = signupService;
    }

    public Guid? BusinessId { get; private set; }

    public void OnGet([FromQuery] string? businessId)
    {
        if (Guid.TryParse(businessId, out var id))
        {
            BusinessId = id;
        }
    }

    public async Task<IActionResult> OnPostAsync([FromForm] string? businessId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(businessId, out var id))
        {
            return RedirectToPage("/Index");
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        try
        {
            var checkoutUrl = await _signupService.CreateOrResumeCheckoutAsync(id, baseUrl, cancellationToken);
            return Redirect(checkoutUrl);
        }
        catch (Exception)
        {
            BusinessId = id;
            return Page();
        }
    }
}
