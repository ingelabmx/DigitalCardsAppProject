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
