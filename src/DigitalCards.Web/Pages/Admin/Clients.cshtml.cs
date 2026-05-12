using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Admin;

[Authorize(Policy = AdminAuth.Policy)]
public sealed class ClientsModel : PageModel
{
    private readonly AdminAppService _adminApp;
    private readonly ILogger<ClientsModel> _logger;

    public ClientsModel(AdminAppService adminApp, ILogger<ClientsModel> logger)
    {
        _adminApp = adminApp;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    public IReadOnlyList<PilotClientDto> Clients { get; private set; } = [];

    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostEnableAsync(
        Guid clientId,
        string? notes,
        CancellationToken cancellationToken)
    {
        return await SetPilotAsync(clientId, notes, isEnabled: true, cancellationToken);
    }

    public async Task<IActionResult> OnPostDisableAsync(
        Guid clientId,
        string? notes,
        CancellationToken cancellationToken)
    {
        return await SetPilotAsync(clientId, notes, isEnabled: false, cancellationToken);
    }

    private async Task<IActionResult> SetPilotAsync(
        Guid clientId,
        string? notes,
        bool isEnabled,
        CancellationToken cancellationToken)
    {
        if (clientId == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "El cliente no existe.");
        }

        if (notes?.Length > 500)
        {
            ModelState.AddModelError(string.Empty, "Las notas no pueden exceder 500 caracteres.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        var result = await _adminApp.SetPilotClientAsync(
            new SetPilotClientCommand(
                clientId,
                AdminAuth.GetAdminUserId(User),
                isEnabled,
                notes),
            cancellationToken);

        if (result is null)
        {
            ModelState.AddModelError(string.Empty, "El cliente no existe.");
            await LoadAsync(cancellationToken);
            return Page();
        }

        _logger.LogInformation(
            "Admin {AdminUserId} changed pilot client {ClientId} enabled {IsEnabled}.",
            AdminAuth.GetAdminUserId(User),
            result.ClientId,
            result.IsEnabled);
        StatusMessage = result.IsEnabled
            ? $"Piloto habilitado para {result.UserName}."
            : $"Piloto deshabilitado para {result.UserName}.";
        await LoadAsync(cancellationToken);
        return Page();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Clients = await _adminApp.ListPilotClientsAsync(Query ?? string.Empty, cancellationToken);
    }
}
