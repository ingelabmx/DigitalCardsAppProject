using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Client;

[Authorize(Policy = ClientAuth.Policy)]
public sealed class ChangePasswordModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/Client/Profile", null, null, "password");
    }

    public IActionResult OnPost()
    {
        return RedirectToPage("/Client/Profile", null, null, "password");
    }
}
