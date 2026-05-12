using DigitalCards.Application.Abstractions;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Business;

[Authorize(Policy = BusinessAuth.Policy)]
public sealed class DashboardModel : PageModel
{
    private readonly IBusinessRepository _businesses;

    public DashboardModel(IBusinessRepository businesses)
    {
        _businesses = businesses;
    }

    public string BusinessName { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var businessId = BusinessAuth.GetBusinessId(User);
        var business = await _businesses.FindByIdAsync(businessId, cancellationToken);
        if (business is null)
        {
            return RedirectToPage("/Business/Logout");
        }

        BusinessName = business.Name;
        return Page();
    }
}
