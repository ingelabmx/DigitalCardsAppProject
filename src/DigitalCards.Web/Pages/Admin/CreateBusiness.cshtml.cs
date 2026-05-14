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
    private readonly DigitalCardsAppService _appService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CreateBusinessModel> _logger;

    public CreateBusinessModel(
        AdminAppService adminApp,
        DigitalCardsAppService appService,
        IConfiguration configuration,
        ILogger<CreateBusinessModel> logger)
    {
        _adminApp = adminApp;
        _appService = appService;
        _configuration = configuration;
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
        var sendInvite = Input.SendInvite;
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
        if (sendInvite)
        {
            await _appService.RequestBusinessPasswordResetAsync(
                new RequestBusinessPasswordResetCommand(CreatedBusiness!.BusinessEmail, GetBaseUrl()),
                cancellationToken);
        }

        StatusMessage = CreatedBusiness!.IsEnabled
            ? $"Negocio creado y habilitado para piloto: {CreatedBusiness.BusinessName}."
            : $"Negocio creado: {CreatedBusiness.BusinessName}.";
        if (sendInvite)
        {
            StatusMessage += " Invitacion enviada por correo para configurar acceso.";
        }

        _logger.LogInformation(
            "Admin {AdminUserId} created business {BusinessId} with pilot enabled {IsPilotEnabled} and invite sent {InviteSent}.",
            adminUserId,
            CreatedBusiness.BusinessId,
            CreatedBusiness.IsEnabled,
            sendInvite);

        Input = new InputModel();
        return Page();
    }

    private void ClearPasswordFields()
    {
        Input.InitialPassword = string.Empty;
        Input.ConfirmPassword = string.Empty;
    }

    private string GetBaseUrl()
    {
        return EnrollmentBaseUrlResolver.Resolve(
            _configuration["DigitalCards:PublicBaseUrl"],
            Request.Scheme,
            Request.Host);
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

        [Display(Name = "Enviar invitacion por correo")]
        public bool SendInvite { get; set; }

        [Display(Name = "Notas")]
        [StringLength(500, ErrorMessage = "Las notas no pueden exceder 500 caracteres.")]
        public string? Notes { get; set; }
    }
}
