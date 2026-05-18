using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Web.Landing;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace DigitalCards.Web.Pages;

public sealed class IndexModel : PageModel
{
    private readonly IEmailSender _emailSender;
    private readonly LandingOptions _landing;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IOptions<LandingOptions> landing,
        IEmailSender emailSender,
        ILogger<IndexModel> logger)
    {
        _landing = landing.Value;
        _emailSender = emailSender;
        _logger = logger;
    }

    public GatewaySession? AdminSession { get; private set; }

    public GatewaySession? BusinessSession { get; private set; }

    public GatewaySession? ClientSession { get; private set; }

    public bool IsLanding { get; private set; }

    public LandingOptions Landing => _landing;

    [BindProperty]
    public ScheduleCallInput ScheduleCall { get; set; } = new();

    [TempData]
    public string? LandingSuccessMessage { get; set; }

    [TempData]
    public string? LandingErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        IsLanding = LandingHost.IsLandingHost(HttpContext, _landing);
        if (IsLanding)
        {
            return;
        }

        AdminSession = await GetSessionAsync(AdminAuth.Scheme, AdminAuth.AdminNameClaim);
        BusinessSession = await GetSessionAsync(BusinessAuth.Scheme, BusinessAuth.BusinessNameClaim);
        ClientSession = await GetSessionAsync(ClientAuth.Scheme, ClientAuth.ClientNameClaim);
    }

    public async Task<IActionResult> OnPostScheduleCallAsync(CancellationToken cancellationToken)
    {
        IsLanding = LandingHost.IsLandingHost(HttpContext, _landing);
        if (!IsLanding)
        {
            return Redirect(_landing.AppUrl);
        }

        if (!ModelState.IsValid)
        {
            LandingErrorMessage = "Revisa los datos del formulario.";
            return Page();
        }

        try
        {
            await _emailSender.SendLandingContactAsync(
                new LandingContactEmail(
                    _landing.ContactEmail,
                    ScheduleCall.Name.Trim(),
                    ScheduleCall.BusinessName.Trim(),
                    ScheduleCall.Email.Trim(),
                    ScheduleCall.Phone.Trim(),
                    "Videollamada por Google Meet",
                    DateTimeOffset.UtcNow),
                cancellationToken);

            LandingSuccessMessage = "Gracias. Recibimos tu solicitud y te contactaremos para coordinar la videollamada.";
            ScheduleCall = new ScheduleCallInput();
            ModelState.Clear();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not send Puntelio landing contact request.");
            LandingErrorMessage = "No pudimos enviar tu solicitud. Intenta nuevamente o contactanos por WhatsApp.";
        }

        return Page();
    }

    public string WhatsAppLink(string message)
    {
        return $"{_landing.WhatsAppUrl}?text={Uri.EscapeDataString(message)}";
    }

    public string MailTo(string subject)
    {
        return $"mailto:{_landing.ContactEmail}?subject={Uri.EscapeDataString(subject)}";
    }

    private async Task<GatewaySession?> GetSessionAsync(string scheme, string displayNameClaim)
    {
        var result = await HttpContext.AuthenticateAsync(scheme);
        if (!result.Succeeded || result.Principal is null)
        {
            return null;
        }

        var displayName = result.Principal.FindFirst(displayNameClaim)?.Value;
        return new GatewaySession(string.IsNullOrWhiteSpace(displayName) ? "sesion activa" : displayName);
    }

    public sealed record GatewaySession(string DisplayName);

    public sealed class ScheduleCallInput
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [Display(Name = "Nombre")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del negocio es obligatorio.")]
        [Display(Name = "Nombre del negocio")]
        public string BusinessName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Captura un correo valido.")]
        [Display(Name = "Correo")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El telefono es obligatorio.")]
        [Display(Name = "Telefono")]
        public string Phone { get; set; } = string.Empty;
    }
}
