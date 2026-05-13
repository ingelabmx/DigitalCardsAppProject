using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Client;

[Authorize(Policy = ClientAuth.Policy)]
public sealed class DashboardModel : PageModel
{
    public string ClientName { get; private set; } = string.Empty;

    public void OnGet()
    {
        ClientName = ClientAuth.GetClientName(User);
    }
}
