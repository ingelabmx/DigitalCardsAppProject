using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Client;

[Authorize(Policy = ClientAuth.Policy)]
public sealed class DashboardModel : PageModel
{
    private readonly DigitalCardsAppService _appService;

    public DashboardModel(DigitalCardsAppService appService)
    {
        _appService = appService;
    }

    public ClientDashboardDto? Dashboard { get; private set; }

    public string ClientName => Dashboard?.Client is { } client
        ? $"{client.FirstName} {client.LastName}"
        : ClientAuth.GetClientName(User);

    public string ClientQrSvg => Dashboard is null
        ? string.Empty
        : EnrollmentQrCodeRenderer.RenderSvg(Dashboard.Client.UserName);

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            Dashboard = await _appService.GetClientDashboardAsync(
                ClientAuth.GetClientId(User),
                cancellationToken);
            return Page();
        }
        catch (InvalidOperationException ex)
            when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            await HttpContext.SignOutAsync();
            return RedirectToPage("/Client/Login");
        }
    }
}
