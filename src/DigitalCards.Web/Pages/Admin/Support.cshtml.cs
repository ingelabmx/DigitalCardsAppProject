using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Infrastructure.LegacySync;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace DigitalCards.Web.Pages.Admin;

[Authorize(Policy = AdminAuth.Policy)]
public sealed class SupportModel : PageModel
{
    private readonly AdminAppService _adminApp;
    private readonly LegacyWalletSyncOptions _legacyWalletSyncOptions;
    private readonly ILogger<SupportModel> _logger;

    public SupportModel(
        AdminAppService adminApp,
        IOptions<LegacyWalletSyncOptions> legacyWalletSyncOptions,
        ILogger<SupportModel> logger)
    {
        _adminApp = adminApp;
        _legacyWalletSyncOptions = legacyWalletSyncOptions.Value;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    public AdminSupportResult? Result { get; private set; }

    public LegacyWalletSyncOptions LegacyWalletSync => _legacyWalletSyncOptions;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Query))
        {
            return;
        }

        Result = await _adminApp.SearchSupportAsync(
            new AdminSupportQuery(Query),
            cancellationToken);

        _logger.LogInformation(
            "Admin {AdminUserId} searched support center with query length {QueryLength}.",
            AdminAuth.GetAdminUserId(User),
            Query.Trim().Length);
    }

    public static string Suffix(Guid id)
    {
        var value = id.ToString("N");
        return value[^8..];
    }
}
