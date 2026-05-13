using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Infrastructure.LegacySync;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    public async Task<IActionResult> OnGetExportAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Query))
        {
            return BadRequest("Query is required.");
        }

        var result = await _adminApp.SearchSupportAsync(
            new AdminSupportQuery(Query),
            cancellationToken);
        var export = new
        {
            generatedAt = DateTimeOffset.UtcNow,
            result.Query,
            legacyWalletSync = new
            {
                _legacyWalletSyncOptions.Enabled,
                _legacyWalletSyncOptions.PollIntervalSeconds,
                _legacyWalletSyncOptions.LookbackMinutes,
                _legacyWalletSyncOptions.BatchSize
            },
            counts = new
            {
                clients = result.Clients.Count,
                businesses = result.Businesses.Count,
                cards = result.Cards.Count
            },
            result.Clients,
            result.Businesses,
            result.Cards
        };
        var json = JsonSerializer.Serialize(
            export,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            });
        var fileName = $"support-diagnostic-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.json";

        _logger.LogInformation(
            "Admin {AdminUserId} exported support diagnostics with query length {QueryLength}.",
            AdminAuth.GetAdminUserId(User),
            Query.Trim().Length);

        return File(
            Encoding.UTF8.GetBytes(json),
            "application/json",
            fileName);
    }

    public static string Suffix(Guid id)
    {
        var value = id.ToString("N");
        return value[^8..];
    }
}
