using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Client;

public sealed class ForgotPasswordModel : PageModel
{
    private readonly DigitalCardsAppService _appService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ForgotPasswordModel> _logger;

    public ForgotPasswordModel(
        DigitalCardsAppService appService,
        IConfiguration configuration,
        ILogger<ForgotPasswordModel> logger)
    {
        _appService = appService;
        _configuration = configuration;
        _logger = logger;
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

        await _appService.RequestClientPasswordResetAsync(
            new RequestClientPasswordResetCommand(Input.UserNameOrEmail, GetBaseUrl()),
            cancellationToken);

        _logger.LogInformation("Client password reset requested for {ClientIdentifier}.", Mask(Input.UserNameOrEmail));
        StatusMessage = "Si existe una cuenta con esos datos, enviaremos un link para restablecer la contrasena.";
        ModelState.Clear();
        Input = new InputModel();
        return Page();
    }

    private string GetBaseUrl()
    {
        return EnrollmentBaseUrlResolver.Resolve(
            _configuration["DigitalCards:PublicBaseUrl"],
            Request.Scheme,
            Request.Host);
    }

    private static string Mask(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        var atIndex = normalized.IndexOf('@');
        return atIndex <= 1 ? "***" : string.Concat(normalized[0], "***", normalized[atIndex..]);
    }

    public sealed class InputModel
    {
        [Display(Name = "Usuario o correo")]
        [Required]
        public string UserNameOrEmail { get; set; } = string.Empty;
    }
}
