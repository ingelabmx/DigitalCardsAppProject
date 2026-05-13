using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages;

public sealed class RegisterModel : PageModel
{
    private readonly DigitalCardsAppService _appService;

    public RegisterModel(DigitalCardsAppService appService)
    {
        _appService = appService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? StatusMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var client = await _appService.RegisterClientAsync(
                new RegisterClientCommand(Input.UserName, Input.FirstName, Input.LastName, Input.Email, Input.Password),
                cancellationToken);

            StatusMessage = $"Cliente {client.UserName} registrado.";
            ModelState.Clear();
            Input = new InputModel();
            return Page();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }

    public sealed class InputModel
    {
        [Display(Name = "Usuario")]
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Display(Name = "Nombre")]
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Apellido")]
        [Required]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Correo")]
        [EmailAddress]
        [Required]
        public string Email { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Contrasena")]
        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;
    }
}
