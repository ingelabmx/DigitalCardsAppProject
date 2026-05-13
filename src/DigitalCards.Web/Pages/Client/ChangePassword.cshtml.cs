using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Client;

[Authorize(Policy = ClientAuth.Policy)]
public sealed class ChangePasswordModel : PageModel
{
    private readonly DigitalCardsAppService _appService;
    private readonly ILogger<ChangePasswordModel> _logger;

    public ChangePasswordModel(
        DigitalCardsAppService appService,
        ILogger<ChangePasswordModel> logger)
    {
        _appService = appService;
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

        if (!string.Equals(Input.NewPassword, Input.ConfirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "La confirmacion no coincide.");
            return Page();
        }

        var clientId = ClientAuth.GetClientId(User);
        var result = await _appService.ChangeClientPasswordAsync(
            new ChangeClientPasswordCommand(clientId, Input.CurrentPassword, Input.NewPassword),
            cancellationToken);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Client password change failed for client {ClientId}.", clientId);
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo cambiar la contrasena.");
            return Page();
        }

        _logger.LogInformation("Client password changed for client {ClientId}.", clientId);
        StatusMessage = "Contrasena de cliente actualizada.";
        ModelState.Clear();
        Input = new InputModel();
        return Page();
    }

    public sealed class InputModel
    {
        [DataType(DataType.Password)]
        [Display(Name = "Contrasena actual")]
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

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
