using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Admin;

[Authorize(Policy = AdminAuth.Policy)]
public sealed class ReportsModel : PageModel
{
    private readonly AdminAppService _adminApp;
    private readonly ILogger<ReportsModel> _logger;

    public ReportsModel(AdminAppService adminApp, ILogger<ReportsModel> logger)
    {
        _adminApp = adminApp;
        _logger = logger;
    }

    public AdminReportsDto? Reports { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Reports = await _adminApp.GetReportsAsync(cancellationToken);
        _logger.LogInformation(
            "Admin {AdminUserId} viewed reports dashboard.",
            AdminAuth.GetAdminUserId(User));
    }
}
