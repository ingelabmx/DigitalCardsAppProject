using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
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

    public void OnGet()
    {
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

        return RedirectToPage("/Business/Dashboard", new { businessId = business.Id });
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

