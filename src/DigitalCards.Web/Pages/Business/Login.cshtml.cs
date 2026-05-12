using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Business;

public sealed class LoginModel : PageModel
{
    private readonly DigitalCardsAppService _appService;

    public LoginModel(DigitalCardsAppService appService)
    {
        _appService = appService;
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
            ModelState.AddModelError(string.Empty, "Credenciales de negocio invalidas.");
            return Page();
        }

        await HttpContext.SignInAsync(
            BusinessAuth.Scheme,
            BusinessAuth.CreatePrincipal(business),
            new AuthenticationProperties
            {
                IsPersistent = false,
                IssuedUtc = DateTimeOffset.UtcNow
            });

        return RedirectToPage("/Business/Dashboard");
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
