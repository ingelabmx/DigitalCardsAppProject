using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Admin;

[Authorize(Policy = AdminAuth.Policy)]
public sealed class CreateBusinessModel : PageModel
{
    private readonly AdminAppService _adminApp;
    private readonly ILogger<CreateBusinessModel> _logger;

    public CreateBusinessModel(AdminAppService adminApp, ILogger<CreateBusinessModel> logger)
    {
        _adminApp = adminApp;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public PilotBusinessDto? CreatedBusiness { get; private set; }

    public string? StatusMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ClearPasswordFields();
            return Page();
        }

        var adminUserId = AdminAuth.GetAdminUserId(User);
        var result = await _adminApp.CreateBusinessAsync(
            new CreateBusinessCommand(
                Input.BusinessName,
                Input.BusinessEmail,
                Input.InitialPassword,
                adminUserId,
                Input.EnablePilot,
                Input.Notes),
            cancellationToken);

        ClearPasswordFields();

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo crear el negocio.");
            return Page();
        }

        CreatedBusiness = result.Business;
        StatusMessage = CreatedBusiness!.IsEnabled
            ? $"Negocio creado y habilitado para piloto: {CreatedBusiness.BusinessName}."
            : $"Negocio creado: {CreatedBusiness.BusinessName}.";

        _logger.LogInformation(
            "Admin {AdminUserId} created business {BusinessId} with pilot enabled {IsPilotEnabled}.",
            adminUserId,
            CreatedBusiness.BusinessId,
            CreatedBusiness.IsEnabled);

        Input = new InputModel();
        return Page();
    }

    private void ClearPasswordFields()
    {
        Input.InitialPassword = string.Empty;
        Input.ConfirmPassword = string.Empty;
    }

    public sealed class InputModel
    {
        [Display(Name = "Nombre del negocio")]
        [Required(ErrorMessage = "El nombre del negocio es requerido.")]
        [StringLength(30, ErrorMessage = "El nombre del negocio no puede exceder 30 caracteres.")]
        public string BusinessName { get; set; } = string.Empty;

        [Display(Name = "Correo del negocio")]
        [EmailAddress(ErrorMessage = "El correo del negocio no es valido.")]
        [Required(ErrorMessage = "El correo del negocio es requerido.")]
        [StringLength(30, ErrorMessage = "El correo del negocio no puede exceder 30 caracteres.")]
        public string BusinessEmail { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Contrasena inicial")]
        [Required(ErrorMessage = "La contrasena inicial es requerida.")]
        [MinLength(8, ErrorMessage = "La contrasena inicial debe tener al menos 8 caracteres.")]
        [StringLength(128, ErrorMessage = "La contrasena inicial no puede exceder 128 caracteres.")]
        public string InitialPassword { get; set; } = string.Empty;

        [Compare(nameof(InitialPassword), ErrorMessage = "Las contrasenas no coinciden.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contrasena")]
        [Required(ErrorMessage = "Confirma la contrasena inicial.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Habilitar piloto")]
        public bool EnablePilot { get; set; }

        [Display(Name = "Notas")]
        [StringLength(500, ErrorMessage = "Las notas no pueden exceder 500 caracteres.")]
        public string? Notes { get; set; }
    }
}
