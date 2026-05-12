using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Admin;

[Authorize(Policy = AdminAuth.Policy)]
public sealed class DashboardModel : PageModel
{
    public string AdminName { get; private set; } = string.Empty;

    public void OnGet()
    {
        AdminName = AdminAuth.GetAdminName(User);
    }
}
