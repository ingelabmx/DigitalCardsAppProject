using DigitalCards.Application.Abstractions;
using DigitalCards.Web.Pilot;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Business;

[Authorize(Policy = BusinessAuth.Policy)]
public sealed class DashboardModel : PageModel
{
    private readonly IBusinessRepository _businesses;
    private readonly PilotAccessService _pilotAccess;

    public DashboardModel(
        IBusinessRepository businesses,
        PilotAccessService pilotAccess)
    {
        _businesses = businesses;
        _pilotAccess = pilotAccess;
    }

    public string BusinessName { get; private set; } = string.Empty;

    public string? PilotBlockMessage { get; private set; }

    public bool IsPilotBlocked => PilotBlockMessage is not null;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var businessId = BusinessAuth.GetBusinessId(User);
        var business = await _businesses.FindByIdAsync(businessId, cancellationToken);
        if (business is null)
        {
            return RedirectToPage("/Business/Logout");
        }

        BusinessName = business.Name;
        var pilotAccess = await _pilotAccess.CheckBusinessAsync(business.Id, business.Email, cancellationToken);
        if (!pilotAccess.IsAllowed)
        {
            PilotBlockMessage = pilotAccess.Message;
        }

        return Page();
    }
}
