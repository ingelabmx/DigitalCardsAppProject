using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Pilot;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Business;

[Authorize(Policy = BusinessAuth.Policy)]
public sealed class ReportsModel : PageModel
{
    private readonly DigitalCardsAppService _appService;
    private readonly PilotAccessService _pilotAccess;
    private readonly ILogger<ReportsModel> _logger;

    public ReportsModel(
        DigitalCardsAppService appService,
        PilotAccessService pilotAccess,
        ILogger<ReportsModel> logger)
    {
        _appService = appService;
        _pilotAccess = pilotAccess;
        _logger = logger;
    }

    public BusinessReportsDto? Reports { get; private set; }

    public string? PilotBlockMessage { get; private set; }

    public bool IsPilotBlocked => PilotBlockMessage is not null;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var businessId = BusinessAuth.GetBusinessId(User);
        Reports = await _appService.GetBusinessReportsAsync(businessId, cancellationToken);
        if (Reports is null)
        {
            return RedirectToPage("/Business/Logout");
        }

        var pilotAccess = await _pilotAccess.CheckBusinessAsync(
            Reports.Business.Id,
            Reports.Business.Email,
            cancellationToken);
        if (!pilotAccess.IsAllowed)
        {
            PilotBlockMessage = pilotAccess.Message;
        }

        _logger.LogInformation(
            "Business {BusinessId} opened business reports with {CardCount} card(s).",
            businessId,
            Reports.CardCount);

        return Page();
    }
}
