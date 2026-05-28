using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Pilot;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Business;

[EnableRateLimiting(SecurityRateLimitPolicyNames.Auth)]
public sealed class LoginModel : PageModel
{
    private readonly DigitalCardsAppService _appService;
    private readonly PilotAccessService _pilotAccess;
    private readonly IBusinessSubscriptionRepository _subscriptions;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        DigitalCardsAppService appService,
        PilotAccessService pilotAccess,
        IBusinessSubscriptionRepository subscriptions,
        ILogger<LoginModel> logger)
    {
        _appService = appService;
        _pilotAccess = pilotAccess;
        _subscriptions = subscriptions;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IActionResult OnGet()
    {
        return User.HasClaim(claim => claim.Type == BusinessAuth.BusinessIdClaim)
            ? RedirectToPage("/Business/Dashboard")
            : Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var business = await _appService.LoginBusinessAsync(
            new BusinessLoginCommand(Input.Email, Input.Password),
            cancellationToken);

        if (business is null)
        {
            _logger.LogWarning("Business login failed for {BusinessEmail}.", MaskEmail(Input.Email));
            ModelState.AddModelError(string.Empty, "Credenciales de negocio invalidas.");
            return Page();
        }

        var pilotAccess = await _pilotAccess.CheckBusinessLoginAsync(business.Id, cancellationToken);
        if (!pilotAccess.IsAllowed)
        {
            var sub = await _subscriptions.FindByBusinessIdAsync(business.Id, cancellationToken);
            if (sub is { CreatedViaSelfService: true } &&
                sub.SubscriptionStatus is "canceled" or "past_due" or "pending_payment")
            {
                TempData["ReactivateBusinessId"] = business.Id.ToString();
                return RedirectToPage("/Business/Reactivate");
            }
            _logger.LogWarning("Business login blocked for business {BusinessId}.", business.Id);
            ModelState.AddModelError(string.Empty, pilotAccess.Message ?? "El negocio no puede usar Puntelio.");
            return Page();
        }

        _logger.LogInformation("Business login succeeded for business {BusinessId}.", business.Id);
        await HttpContext.SignInAsync(
            BusinessAuth.Scheme,
            BusinessAuth.CreatePrincipal(business),
            new AuthenticationProperties
            {
                IsPersistent = true,
                IssuedUtc = DateTimeOffset.UtcNow
            });

        return RedirectToPage("/Business/Dashboard");
    }

    private static string MaskEmail(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var atIndex = normalized.IndexOf('@');
        return atIndex <= 1 ? "***" : string.Concat(normalized[0], "***", normalized[atIndex..]);
    }

    public sealed class InputModel
    {
        [Display(Name = "Correo")]
        [EmailAddress]
        [Required]
        public string Email { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Contrasena")]
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
