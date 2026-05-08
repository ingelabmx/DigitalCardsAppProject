using DigitalCards.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Business;

public sealed class DashboardModel : PageModel
{
    private readonly IBusinessRepository _businesses;

    public DashboardModel(IBusinessRepository businesses)
    {
        _businesses = businesses;
    }

    public Guid BusinessId { get; private set; }

    public string BusinessName { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var business = await _businesses.FindByIdAsync(businessId, cancellationToken);
        if (business is null)
        {
            return RedirectToPage("/Business/Login");
        }

        BusinessId = business.Id;
        BusinessName = business.Name;
        return Page();
    }
}

