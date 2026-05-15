using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Domain;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Admin;

[Authorize(Policy = AdminAuth.Policy)]
public sealed class AuditModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public OperationalAuditEventType? EventType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTimeOffset? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTimeOffset? To { get; set; }

    public IActionResult OnGet()
    {
        return RedirectToPage("/Admin/Support", new
        {
            AuditEventType = EventType,
            AuditSearch = Search,
            AuditFrom = From,
            AuditTo = To
        });
    }

    public static string Suffix(Guid? id)
    {
        if (id is null)
        {
            return "-";
        }

        var value = id.Value.ToString("N");
        return value[^8..];
    }
}
