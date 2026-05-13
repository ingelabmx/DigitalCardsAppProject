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

    public ClientDto? Client { get; private set; }

    public string? StatusMessage { get; private set; }

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

        if (!ModelState.IsValid)
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
}
