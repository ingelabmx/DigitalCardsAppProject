using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Client;

[EnableRateLimiting(SecurityRateLimitPolicyNames.Auth)]
public sealed class LoginModel : PageModel
{
    private readonly DigitalCardsAppService _appService;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(DigitalCardsAppService appService, ILogger<LoginModel> logger)
    {
        _appService = appService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var result = await HttpContext.AuthenticateAsync(ClientAuth.Scheme);
        return result.Succeeded
            ? RedirectToPage("/Client/Dashboard")
            : Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var client = await _appService.LoginClientAsync(
            new ClientLoginCommand(Input.UserNameOrEmail, Input.Password),
            cancellationToken);

        if (client is null)
        {
            _logger.LogWarning("Client login failed for {ClientIdentifier}.", Mask(Input.UserNameOrEmail));
            ModelState.AddModelError(string.Empty, "Credenciales de cliente invalidas.");
            return Page();
        }

        _logger.LogInformation("Client login succeeded for client {ClientId}.", client.Id);
        await HttpContext.SignInAsync(
            ClientAuth.Scheme,
            ClientAuth.CreatePrincipal(client),
            new AuthenticationProperties
            {
                IsPersistent = false,
                IssuedUtc = DateTimeOffset.UtcNow
            });

        return RedirectToPage("/Client/Dashboard");
    }

    private static string Mask(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        var atIndex = normalized.IndexOf('@');
        return atIndex <= 1 ? "***" : string.Concat(normalized[0], "***", normalized[atIndex..]);
    }

    public sealed class InputModel
    {
        [Display(Name = "Usuario o correo")]
        [Required]
        public string UserNameOrEmail { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Contrasena")]
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
