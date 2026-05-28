using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Business;

[AllowAnonymous]
public sealed class SignupModel : PageModel
{
    private readonly BusinessSignupService _signupService;

    public SignupModel(BusinessSignupService signupService)
    {
        _signupService = signupService;
    }

    [BindProperty]
    public SignupInputModel Input { get; set; } = new();

    [TempData]
    public string? ErrorMessage { get; set; }

    public void OnGet([FromQuery] string? plan)
    {
        Input.PlanKey = plan switch
        {
            "Pro" => "Pro",
            "Business" => "Business",
            _ => "Basic"
        };
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (Input.Password != Input.ConfirmPassword)
        {
            ModelState.AddModelError(nameof(Input.ConfirmPassword), "Las contrasenas no coinciden.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _signupService.SignupAsync(
            new SignupCommand(Input.BusinessName, Input.BusinessEmail, Input.Password, Input.PlanKey),
            cancellationToken);

        if (!result.Succeeded)
        {
            ErrorMessage = result.ErrorMessage;
            return Page();
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        try
        {
            var checkoutUrl = await _signupService.CreateOrResumeCheckoutAsync(result.BusinessId!.Value, baseUrl, cancellationToken);
            return Redirect(checkoutUrl);
        }
        catch (Exception)
        {
            ErrorMessage = "El negocio fue creado pero no se pudo iniciar el pago. Intenta de nuevo.";
            return RedirectToPage("/Stripe/Cancel", new { businessId = result.BusinessId!.Value });
        }
    }

    public sealed class SignupInputModel
    {
        [Required(ErrorMessage = "El nombre del negocio es requerido.")]
        [MaxLength(30, ErrorMessage = "El nombre no puede exceder 30 caracteres.")]
        public string BusinessName { get; set; } = "";

        [Required(ErrorMessage = "El correo es requerido.")]
        [EmailAddress(ErrorMessage = "Correo no valido.")]
        [MaxLength(30, ErrorMessage = "El correo no puede exceder 30 caracteres.")]
        public string BusinessEmail { get; set; } = "";

        [Required(ErrorMessage = "La contrasena es requerida.")]
        [MinLength(8, ErrorMessage = "La contrasena debe tener al menos 8 caracteres.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = "";

        [Required(ErrorMessage = "Elige un plan.")]
        public string PlanKey { get; set; } = "Basic";
    }
}
