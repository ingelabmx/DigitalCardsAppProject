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
    private readonly AdminAppService _adminApp;

    public AuditModel(AdminAppService adminApp)
    {
        _adminApp = adminApp;
    }

    [BindProperty(SupportsGet = true)]
    public OperationalAuditEventType? EventType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTimeOffset? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTimeOffset? To { get; set; }

    public IReadOnlyList<AdminAuditEventDto> Events { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Events = await _adminApp.SearchAuditAsync(
            new AdminAuditQuery(EventType, Search, From, To, Limit: 100),
            cancellationToken);
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
