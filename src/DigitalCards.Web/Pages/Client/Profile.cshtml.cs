using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Client;

[Authorize(Policy = ClientAuth.Policy)]
public sealed class ProfileModel : PageModel
{
    private readonly DigitalCardsAppService _appService;
    private readonly ILogger<ProfileModel> _logger;

    public ProfileModel(
        DigitalCardsAppService appService,
        ILogger<ProfileModel> logger)
    {
        _appService = appService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public PasswordInputModel PasswordInput { get; set; } = new();

    public ClientDto? Client { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? PasswordStatusMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        return await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Client = await _appService.GetClientProfileAsync(ClientAuth.GetClientId(User), cancellationToken);
        if (Client is null)
        {
            return RedirectToPage("/Client/Logout");
        }

        ModelState.Clear();
        if (!ValidateFormModel(Input, nameof(Input)))
        {
            return Page();
        }

        var result = await _appService.UpdateClientProfileAsync(
            new UpdateClientProfileCommand(
                Client.Id,
                Input.FirstName,
                Input.LastName,
                Input.Email),
            cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo actualizar el perfil.");
            return Page();
        }

        Client = result.Client;
        Input = new InputModel
        {
            FirstName = Client!.FirstName,
            LastName = Client.LastName,
            Email = Client.Email
        };
        await HttpContext.SignInAsync(ClientAuth.Scheme, ClientAuth.CreatePrincipal(Client));

        _logger.LogInformation("Client {ClientId} updated profile.", Client.Id);
        StatusMessage = "Perfil actualizado.";
        ModelState.Clear();
        return Page();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync(CancellationToken cancellationToken)
    {
        Client = await _appService.GetClientProfileAsync(ClientAuth.GetClientId(User), cancellationToken);
        if (Client is null)
        {
            return RedirectToPage("/Client/Logout");
        }

        Input = new InputModel
        {
            FirstName = Client.FirstName,
            LastName = Client.LastName,
            Email = Client.Email
        };
        ModelState.Clear();
        if (!ValidateFormModel(PasswordInput, nameof(PasswordInput)))
        {
            return Page();
        }

        if (!string.Equals(PasswordInput.NewPassword, PasswordInput.ConfirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "La confirmacion no coincide.");
            return Page();
        }

        var result = await _appService.ChangeClientPasswordAsync(
            new ChangeClientPasswordCommand(Client.Id, PasswordInput.CurrentPassword, PasswordInput.NewPassword),
            cancellationToken);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Client password change failed for client {ClientId}.", Client.Id);
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo cambiar la contrasena.");
            return Page();
        }

        _logger.LogInformation("Client password changed for client {ClientId}.", Client.Id);
        PasswordStatusMessage = "Contrasena de cliente actualizada.";
        PasswordInput = new PasswordInputModel();
        ModelState.Clear();
        return Page();
    }

    private async Task<IActionResult> LoadAsync(CancellationToken cancellationToken)
    {
        Client = await _appService.GetClientProfileAsync(ClientAuth.GetClientId(User), cancellationToken);
        if (Client is null)
        {
            return RedirectToPage("/Client/Logout");
        }

        Input = new InputModel
        {
            FirstName = Client.FirstName,
            LastName = Client.LastName,
            Email = Client.Email
        };
        return Page();
    }

    private bool ValidateFormModel(object model, string prefix)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model);
        if (Validator.TryValidateObject(model, context, validationResults, validateAllProperties: true))
        {
            return true;
        }

        foreach (var result in validationResults)
        {
            var members = result.MemberNames.DefaultIfEmpty(string.Empty);
            foreach (var member in members)
            {
                var key = string.IsNullOrWhiteSpace(member) ? string.Empty : $"{prefix}.{member}";
                ModelState.AddModelError(key, result.ErrorMessage ?? "El valor no es valido.");
            }
        }

        return false;
    }

    public sealed class InputModel
    {
        [Display(Name = "Nombre")]
        [Required]
        [MaxLength(30)]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Apellido")]
        [Required]
        [MaxLength(30)]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Correo")]
        [Required]
        [EmailAddress]
        [MaxLength(30)]
        public string Email { get; set; } = string.Empty;
    }

    public sealed class PasswordInputModel
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
