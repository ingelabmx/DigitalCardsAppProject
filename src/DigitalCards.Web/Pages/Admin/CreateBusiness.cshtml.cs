using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Domain;
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
    private readonly IBusinessSubscriptionRepository _subscriptions;
    private readonly IConfiguration _configuration;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<CreateBusinessModel> _logger;

    public CreateBusinessModel(
        AdminAppService adminApp,
        DigitalCardsAppService appService,
        IBusinessSubscriptionRepository subscriptions,
        IConfiguration configuration,
        IEmailSender emailSender,
        ILogger<CreateBusinessModel> logger)
    {
        _adminApp = adminApp;
        _appService = appService;
        _subscriptions = subscriptions;
        _configuration = configuration;
        _emailSender = emailSender;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public PilotBusinessDto? CreatedBusiness { get; private set; }

    public string? StatusMessage { get; private set; }

    public void OnGet()
    {
        Input.SendInvite = true;
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
                Notes: null),
            cancellationToken);

        ClearPasswordFields();

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo crear el negocio.");
            return Page();
        }

        CreatedBusiness = result.Business;

        var maxClients = Input.PlanKey switch
        {
            "Basic"    => 300,
            "Pro"      => 1000,
            "Business" => -1,
            _          => -1
        };
        var now = DateTimeOffset.UtcNow;
        await _subscriptions.UpsertAsync(
            new BusinessSubscription(
                CreatedBusiness!.BusinessId,
                subscriptionStatus: "manual",
                maxClients: maxClients,
                createdViaSelfService: false,
                createdAt: now,
                updatedAt: now,
                stripePlanKey: Input.PlanKey),
            cancellationToken);

        var planLabel = Input.PlanKey switch
        {
            "Basic"    => "Basico",
            "Pro"      => "Pro",
            "Business" => "Empresarial",
            _          => "Manual"
        };

        try
        {
            await _emailSender.SendBusinessWelcomeAsync(
                new BusinessWelcomeEmail(
                    CreatedBusiness!.BusinessEmail,
                    CreatedBusiness.BusinessName,
                    planLabel,
                    "https://app.puntelio.com/Business/Login"),
                cancellationToken);
            _logger.LogInformation(
                "Welcome email sent for manually created business {BusinessId}.",
                CreatedBusiness.BusinessId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to send welcome email for business {BusinessId}.",
                CreatedBusiness!.BusinessId);
        }

        if (sendInvite)
        {
            try
            {
                await _appService.RequestBusinessPasswordResetAsync(
                    new RequestBusinessPasswordResetCommand(CreatedBusiness!.BusinessEmail, GetBaseUrl()),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to send password reset email for business {BusinessId}.",
                    CreatedBusiness!.BusinessId);
            }
        }

        StatusMessage = CreatedBusiness!.IsEnabled
            ? $"Negocio creado y activado: {CreatedBusiness.BusinessName}."
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

        [Display(Name = "Plan")]
        public string PlanKey { get; set; } = "Manual";

        [Display(Name = "Activar negocio")]
        public bool EnablePilot { get; set; }

        [Display(Name = "Enviar invitacion por correo")]
        public bool SendInvite { get; set; } = true;

    }
}
