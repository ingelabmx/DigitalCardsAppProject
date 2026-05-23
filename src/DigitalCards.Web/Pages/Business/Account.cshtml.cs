using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Business;

[Authorize(Policy = BusinessAuth.Policy)]
public sealed class AccountModel : PageModel
{
    private readonly DigitalCardsAppService _appService;

    public AccountModel(DigitalCardsAppService appService)
    {
        _appService = appService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? BusinessName { get; private set; }

    public string? StatusMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var businessId = BusinessAuth.GetBusinessId(User);
        var settings = await _appService.GetBusinessBrandingSettingsAsync(businessId, cancellationToken);
        if (settings is null)
        {
            return RedirectToPage("/Business/Logout");
        }

        BusinessName = settings.BusinessName;
        ViewData["BusinessShellName"] = settings.Branding.PublicName;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var businessId = BusinessAuth.GetBusinessId(User);
        var settings = await _appService.GetBusinessBrandingSettingsAsync(businessId, cancellationToken);
        if (settings is null)
        {
            return RedirectToPage("/Business/Logout");
        }

        BusinessName = settings.BusinessName;
        ViewData["BusinessShellName"] = settings.Branding.PublicName;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _appService.ChangeBusinessPasswordAsync(
            new ChangeBusinessPasswordCommand(
                businessId,
                Input.CurrentPassword,
                Input.NewPassword),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo cambiar la contrasena.");
            return Page();
        }

        Input = new InputModel();
        StatusMessage = "Contrasena actualizada correctamente. Se envio un correo de confirmacion.";
        return Page();
    }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "La contrasena actual es requerida.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contrasena nueva es requerida.")]
        [MinLength(8, ErrorMessage = "La contrasena nueva debe tener al menos 8 caracteres.")]
        [MaxLength(128, ErrorMessage = "La contrasena nueva no puede exceder 128 caracteres.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirma la contrasena nueva.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Las contrasenas no coinciden.")]
        [DataType(DataType.Password)]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
