using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Admin;

public sealed class LoginModel : PageModel
{
    private readonly AdminAppService _adminApp;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(AdminAppService adminApp, ILogger<LoginModel> logger)
    {
        _adminApp = adminApp;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var result = await HttpContext.AuthenticateAsync(AdminAuth.Scheme);
        return result.Succeeded
            ? RedirectToPage("/Admin/Dashboard")
            : Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var admin = await _adminApp.LoginAdminAsync(
            new AdminLoginCommand(Input.UserNameOrEmail, Input.Password),
            cancellationToken);

        if (admin is null)
        {
            _logger.LogWarning("Admin login failed for {AdminIdentifier}.", Mask(Input.UserNameOrEmail));
            ModelState.AddModelError(string.Empty, "Credenciales de admin invalidas.");
            return Page();
        }

        _logger.LogInformation("Admin login succeeded for admin {AdminUserId}.", admin.Id);
        await HttpContext.SignInAsync(
            AdminAuth.Scheme,
            AdminAuth.CreatePrincipal(admin),
            new AuthenticationProperties
            {
                IsPersistent = false,
                IssuedUtc = DateTimeOffset.UtcNow
            });

        return RedirectToPage("/Admin/Dashboard");
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
