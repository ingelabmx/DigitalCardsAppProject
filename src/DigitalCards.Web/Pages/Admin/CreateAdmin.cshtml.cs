using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Admin;

[Authorize(Policy = AdminAuth.Policy)]
public sealed class CreateAdminModel : PageModel
{
    private readonly AdminAppService _adminApp;
    private readonly ILogger<CreateAdminModel> _logger;

    public CreateAdminModel(AdminAppService adminApp, ILogger<CreateAdminModel> logger)
    {
        _adminApp = adminApp;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public AdminUserDto? CreatedAdmin { get; private set; }

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

        var actingAdminUserId = AdminAuth.GetAdminUserId(User);
        var result = await _adminApp.CreateAdminAsync(
            new CreateAdminCommand(
                Input.UserName,
                Input.FirstName,
                Input.LastName,
                Input.Email,
                Input.InitialPassword,
                actingAdminUserId),
            cancellationToken);

        ClearPasswordFields();

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo crear el admin.");
            return Page();
        }

        CreatedAdmin = result.Admin;
        StatusMessage = $"Admin creado: {CreatedAdmin!.UserName}.";
        _logger.LogInformation(
            "Admin {AdminUserId} created admin user {CreatedAdminUserId}.",
            actingAdminUserId,
            CreatedAdmin.Id);

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
        [Display(Name = "Usuario")]
        [Required(ErrorMessage = "El usuario admin es requerido.")]
        [StringLength(15, ErrorMessage = "El usuario admin no puede exceder 15 caracteres.")]
        public string UserName { get; set; } = string.Empty;

        [Display(Name = "Nombre")]
        [Required(ErrorMessage = "El nombre del admin es requerido.")]
        [StringLength(30, ErrorMessage = "El nombre del admin no puede exceder 30 caracteres.")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Apellido")]
        [Required(ErrorMessage = "El apellido del admin es requerido.")]
        [StringLength(30, ErrorMessage = "El apellido del admin no puede exceder 30 caracteres.")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Correo")]
        [EmailAddress(ErrorMessage = "El correo del admin no es valido.")]
        [Required(ErrorMessage = "El correo del admin es requerido.")]
        [StringLength(30, ErrorMessage = "El correo del admin no puede exceder 30 caracteres.")]
        public string Email { get; set; } = string.Empty;

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
    }
}
