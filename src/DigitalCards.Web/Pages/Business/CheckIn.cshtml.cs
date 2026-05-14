using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Business;

[Authorize(Policy = BusinessAuth.Policy)]
public sealed class CheckInModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/Business/Cards");
    }

    public IActionResult OnPost()
    {
        return RedirectToPage("/Business/Cards");
    }

    public IActionResult OnPostLookup()
    {
        return RedirectToPage("/Business/Cards");
    }

    public IActionResult OnPostStamp(Guid cardId)
    {
        return RedirectToPage("/Business/Cards", new { cardId });
    }
}
