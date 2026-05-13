using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Business;

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

        await _appService.RequestBusinessPasswordResetAsync(
            new RequestBusinessPasswordResetCommand(Input.Email, GetBaseUrl()),
            cancellationToken);

        _logger.LogInformation("Business password reset requested for {BusinessEmail}.", MaskEmail(Input.Email));
        StatusMessage = "Si existe una cuenta con ese correo, enviaremos un link para restablecer la contrasena.";
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

    private static string MaskEmail(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var atIndex = normalized.IndexOf('@');
        return atIndex <= 1 ? "***" : string.Concat(normalized[0], "***", normalized[atIndex..]);
    }

    public sealed class InputModel
    {
        [Display(Name = "Correo")]
        [EmailAddress]
        [Required]
        public string Email { get; set; } = string.Empty;
    }
}
