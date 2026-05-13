using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Diagnostics;
using DigitalCards.Web.Pilot;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Business;

[Authorize(Policy = BusinessAuth.Policy)]
public sealed class DashboardModel : PageModel
{
    private readonly DigitalCardsAppService _appService;
    private readonly PilotAccessService _pilotAccess;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public DashboardModel(
        DigitalCardsAppService appService,
        PilotAccessService pilotAccess,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _appService = appService;
        _pilotAccess = pilotAccess;
        _environment = environment;
        _configuration = configuration;
    }

    public string BusinessName { get; private set; } = string.Empty;

    public BusinessDashboardDto? Dashboard { get; private set; }

    public string? PilotBlockMessage { get; private set; }

    public bool IsPilotBlocked => PilotBlockMessage is not null;

    public bool ShowDevOutboxLink => DevToolAccess.ShouldRenderDevOutboxLink(
        _environment,
        _configuration,
        User);

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var businessId = BusinessAuth.GetBusinessId(User);
        Dashboard = await _appService.GetBusinessDashboardAsync(businessId, cancellationToken);
        if (Dashboard is null)
        {
            return RedirectToPage("/Business/Logout");
        }

        BusinessName = Dashboard.Business.Name;
        var pilotAccess = await _pilotAccess.CheckBusinessAsync(
            Dashboard.Business.Id,
            Dashboard.Business.Email,
            cancellationToken);
        if (!pilotAccess.IsAllowed)
        {
            PilotBlockMessage = pilotAccess.Message;
        }

        return Page();
    }
}
