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
        return await ShowRetiredAllowlistMessageAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostDisableAsync(
        Guid clientId,
        string? notes,
        CancellationToken cancellationToken)
    {
        return await ShowRetiredAllowlistMessageAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        Guid clientId,
        string confirmation,
        CancellationToken cancellationToken)
    {
        var result = await _adminApp.DeleteClientPermanentlyAsync(
            new DeleteClientCommand(
                clientId,
                AdminAuth.GetAdminUserId(User),
                confirmation),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo eliminar el cliente.");
            await LoadAsync(cancellationToken);
            return Page();
        }

        _logger.LogInformation(
            "Admin {AdminUserId} permanently deleted client {ClientId}.",
            AdminAuth.GetAdminUserId(User),
            clientId);
        StatusMessage = "Cliente eliminado permanentemente. Sus tarjetas y datos Wallet relacionados fueron removidos.";
        await LoadAsync(cancellationToken);
        return Page();
    }

    private async Task<IActionResult> ShowRetiredAllowlistMessageAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Admin {AdminUserId} attempted to change retired client pilot allowlist.",
            AdminAuth.GetAdminUserId(User));
        StatusMessage = "La allowlist de clientes ya no se usa. Activa el negocio desde Admin > Negocios.";
        await LoadAsync(cancellationToken);
        return Page();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Clients = await _adminApp.ListPilotClientsAsync(Query ?? string.Empty, cancellationToken);
    }
}
