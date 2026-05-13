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

    [BindProperty(SupportsGet = true)]
    public string? BusinessFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ClientFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool WalletIssuesOnly { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTimeOffset? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTimeOffset? To { get; set; }

    public AdminSupportResult? Result { get; private set; }

    public LegacyWalletSyncOptions LegacyWalletSync => _legacyWalletSyncOptions;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (!HasSupportCriteria())
        {
            return;
        }

        Result = await _adminApp.SearchSupportAsync(CreateSupportQuery(), cancellationToken);

        _logger.LogInformation(
            "Admin {AdminUserId} searched support center with query length {QueryLength} and filters enabled {HasFilters}.",
            AdminAuth.GetAdminUserId(User),
            Query?.Trim().Length ?? 0,
            HasSupportFilters());
    }

    public async Task<IActionResult> OnGetExportAsync(CancellationToken cancellationToken)
    {
        if (!HasSupportCriteria())
        {
            return BadRequest("Query or filter is required.");
        }

        var result = await _adminApp.SearchSupportAsync(CreateSupportQuery(), cancellationToken);
        var export = new
        {
            generatedAt = DateTimeOffset.UtcNow,
            result.Query,
            filters = new
            {
                BusinessFilter,
                ClientFilter,
                WalletIssuesOnly,
                From,
                To
            },
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
            Query?.Trim().Length ?? 0);

        return File(
            Encoding.UTF8.GetBytes(json),
            "application/json",
            fileName);
    }

    public async Task<IActionResult> OnGetExportCsvAsync(CancellationToken cancellationToken)
    {
        if (!HasSupportCriteria())
        {
            return BadRequest("Query or filter is required.");
        }

        var result = await _adminApp.SearchSupportAsync(CreateSupportQuery(), cancellationToken);
        var csv = new StringBuilder();
        csv.AppendLine("CardSuffix,ClientUserName,BusinessName,CurrentStamps,LifetimeStamps,GoogleIssued,AppleTracked,AppleDevices,WalletIssueCount,LegacySyncEventCount,LastStampedAt");
        foreach (var card in result.Cards)
        {
            csv.Append(Csv(Suffix(card.CardId))).Append(',')
                .Append(Csv(card.Client.UserName)).Append(',')
                .Append(Csv(card.Business.BusinessName)).Append(',')
                .Append(card.CurrentStamps).Append(',')
                .Append(card.LifetimeStamps).Append(',')
                .Append(card.GoogleIssued).Append(',')
                .Append(card.AppleTracked).Append(',')
                .Append(card.AppleRegisteredDeviceCount).Append(',')
                .Append(card.WalletIssueCount).Append(',')
                .Append(card.LegacySyncEventCount).Append(',')
                .Append(Csv(card.LastStampedAt.ToString("o")))
                .AppendLine();
        }

        var fileName = $"support-diagnostic-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.csv";
        _logger.LogInformation(
            "Admin {AdminUserId} exported support diagnostics CSV with query length {QueryLength}.",
            AdminAuth.GetAdminUserId(User),
            Query?.Trim().Length ?? 0);

        return File(
            Encoding.UTF8.GetBytes(csv.ToString()),
            "text/csv",
            fileName);
    }

    public static string Suffix(Guid id)
    {
        var value = id.ToString("N");
        return value[^8..];
    }

    private AdminSupportQuery CreateSupportQuery()
    {
        return new AdminSupportQuery(
            Query ?? string.Empty,
            BusinessFilter,
            ClientFilter,
            WalletIssuesOnly,
            From,
            To);
    }

    private bool HasSupportCriteria()
    {
        return !string.IsNullOrWhiteSpace(Query) || HasSupportFilters();
    }

    private bool HasSupportFilters()
    {
        return !string.IsNullOrWhiteSpace(BusinessFilter) ||
            !string.IsNullOrWhiteSpace(ClientFilter) ||
            WalletIssuesOnly ||
            From.HasValue ||
            To.HasValue;
    }

    private static string Csv(string? value)
    {
        value ??= string.Empty;
        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
