using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Business;

[EnableRateLimiting(SecurityRateLimitPolicyNames.Auth)]
public sealed class ResetPasswordModel : PageModel
{
    private readonly DigitalCardsAppService _appService;
    private readonly ILogger<ResetPasswordModel> _logger;

    public ResetPasswordModel(
        DigitalCardsAppService appService,
        ILogger<ResetPasswordModel> logger)
    {
        _appService = appService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = string.Empty;

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

        if (!string.Equals(Input.NewPassword, Input.ConfirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "La confirmacion no coincide.");
            return Page();
        }

        var result = await _appService.ResetBusinessPasswordAsync(
            new ResetPasswordCommand(Token, Input.NewPassword),
            cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo restablecer la contrasena.");
            return Page();
        }

        _logger.LogInformation("Business password reset completed with token suffix {TokenSuffix}.", SafeSuffix(Token));
        StatusMessage = "Contrasena de negocio actualizada.";
        ModelState.Clear();
        Input = new InputModel();
        return Page();
    }

    private static string SafeSuffix(string token)
    {
        var normalized = token.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? "***"
            : normalized.Length <= 8 ? normalized : normalized[^8..];
    }

    public sealed class InputModel
    {
        [DataType(DataType.Password)]
        [Display(Name = "Contrasena nueva")]
        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contrasena")]
        [Required]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
