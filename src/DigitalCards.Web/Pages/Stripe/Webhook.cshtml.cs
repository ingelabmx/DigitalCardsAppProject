using DigitalCards.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Stripe;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
public sealed class WebhookModel : PageModel
{
    private readonly BusinessSignupService _signupService;
    private readonly ILogger<WebhookModel> _logger;

    public WebhookModel(BusinessSignupService signupService, ILogger<WebhookModel> logger)
    {
        _signupService = signupService;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        string payload;
        using (var reader = new StreamReader(Request.Body, leaveOpen: true))
        {
            payload = await reader.ReadToEndAsync(cancellationToken);
        }

        var stripeSignature = Request.Headers["Stripe-Signature"].ToString();
        if (string.IsNullOrWhiteSpace(stripeSignature))
        {
            _logger.LogWarning("Stripe webhook received without Stripe-Signature header.");
            return BadRequest("Missing Stripe-Signature header.");
        }

        var (success, error) = await _signupService.ProcessWebhookAsync(payload, stripeSignature, cancellationToken);

        if (!success)
        {
            return BadRequest(error ?? "Webhook processing failed.");
        }

        return new OkResult();
    }
}
