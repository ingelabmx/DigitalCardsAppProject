using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Business;

[AllowAnonymous]
public sealed class ReactivateModel : PageModel
{
    private readonly BusinessSignupService _signupService;
    private readonly IBusinessSubscriptionRepository _subscriptions;
    private readonly ILogger<ReactivateModel> _logger;

    public ReactivateModel(
        BusinessSignupService signupService,
        IBusinessSubscriptionRepository subscriptions,
        ILogger<ReactivateModel> logger)
    {
        _signupService = signupService;
        _subscriptions = subscriptions;
        _logger = logger;
    }

    public Guid? BusinessId { get; private set; }
    public string? PlanKey { get; private set; }
    public bool ShowEmailForm { get; private set; }

    [BindProperty]
    public EmailInputModel EmailInput { get; set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (TempData.TryGetValue("ReactivateBusinessId", out var raw) &&
            Guid.TryParse(raw?.ToString(), out var id))
        {
            BusinessId = id;
            var sub = await _subscriptions.FindByBusinessIdAsync(id, cancellationToken);
            PlanKey = sub?.StripePlanKey;
        }
        else
        {
            ShowEmailForm = true;
        }
    }

    public async Task<IActionResult> OnPostAsync([FromForm] string? businessId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(businessId, out var id))
            return RedirectToPage("/Business/Login");

        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var checkoutUrl = await _signupService.CreateOrResumeCheckoutAsync(id, baseUrl, cancellationToken);
            return Redirect(checkoutUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reactivation checkout for business {BusinessId}.", id);
            ModelState.AddModelError(string.Empty, "Ocurrio un error al generar el link de pago. Intenta de nuevo.");
            BusinessId = id;
            return Page();
        }
    }

    public async Task<IActionResult> OnPostByEmailAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ShowEmailForm = true;
            return Page();
        }

        var (foundId, _, error) = await _signupService.FindForReactivationByEmailAsync(EmailInput.Email, cancellationToken);
        if (foundId is null)
        {
            ModelState.AddModelError(string.Empty, error ?? "No se encontro el negocio.");
            ShowEmailForm = true;
            return Page();
        }

        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var checkoutUrl = await _signupService.CreateOrResumeCheckoutAsync(foundId.Value, baseUrl, cancellationToken);
            return Redirect(checkoutUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reactivation checkout by email for business {BusinessId}.", foundId);
            ModelState.AddModelError(string.Empty, "Ocurrio un error al generar el link de pago. Intenta de nuevo.");
            ShowEmailForm = true;
            return Page();
        }
    }

    public sealed class EmailInputModel
    {
        [Required(ErrorMessage = "Ingresa tu correo.")]
        [EmailAddress(ErrorMessage = "Correo invalido.")]
        [Display(Name = "Correo del negocio")]
        public string Email { get; set; } = string.Empty;
    }
}
