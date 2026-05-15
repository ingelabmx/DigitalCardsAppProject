using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Domain;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DigitalCards.Web.Pages.Admin;

[Authorize(Policy = AdminAuth.Policy)]
public sealed class SupportModel : PageModel
{
    private readonly AdminAppService _adminApp;
    private readonly ILogger<SupportModel> _logger;

    public SupportModel(
        AdminAppService adminApp,
        ILogger<SupportModel> logger)
    {
        _adminApp = adminApp;
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

    [BindProperty(SupportsGet = true)]
    public OperationalAuditEventType? AuditEventType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? AuditSearch { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTimeOffset? AuditFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTimeOffset? AuditTo { get; set; }

    [BindProperty]
    public Guid RetryCardId { get; set; }

    public AdminSupportResult? Result { get; private set; }

    public IReadOnlyList<AdminAuditEventDto> AuditEvents { get; private set; } = [];

    public string? StatusMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (!HasSupportCriteria())
        {
            await LoadAuditAsync(cancellationToken);
            return;
        }

        Result = await _adminApp.SearchSupportAsync(CreateSupportQuery(), cancellationToken);
        await LoadAuditAsync(cancellationToken);

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
            counts = new
            {
                clients = result.Clients.Count,
                businesses = result.Businesses.Count,
                cards = result.Cards.Count
            },
            result.Clients,
            result.Businesses,
            cards = result.Cards.Select(card => new
            {
                cardSuffix = Suffix(card.CardId),
                client = new
                {
                    card.Client.UserName,
                    card.Client.ClientEmail
                },
                business = new
                {
                    card.Business.BusinessName,
                    card.Business.BusinessEmail
                },
                card.CurrentStamps,
                card.LifetimeStamps,
                walletReady = card.GoogleIssued || card.AppleTracked,
                registeredDevices = card.AppleRegisteredDeviceCount,
                card.WalletIssueCount,
                card.LastStampedAt,
                card.RecentSafeErrors,
                recentStampEvents = card.RecentStampEvents.Select(item => new
                {
                    source = SourceLabel(item.Source),
                    item.CreatedAt,
                    item.PreviousCheckQTY,
                    item.NewCheckQTY,
                    item.PreviousHistoricCheckQTY,
                    item.NewHistoricCheckQTY,
                    item.ErrorSummary
                }),
                recentRewardRedemptions = card.RecentRewardRedemptions.Select(item => new
                {
                    item.RedeemedAt,
                    item.StampGoal,
                    item.RedeemedCheckQTY,
                    item.HistoricCheckQTY,
                    item.RewardText,
                    item.ErrorSummary
                })
            })
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

        await _adminApp.RecordSupportExportAsync(
            new RecordSupportExportAuditCommand(
                AdminAuth.GetAdminUserId(User),
                "json",
                Query ?? string.Empty,
                result.Cards.Count),
            cancellationToken);

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
        csv.AppendLine("CardSuffix,ClientUserName,BusinessName,CurrentStamps,LifetimeStamps,WalletReady,RegisteredDevices,WalletIssueCount,LastStampedAt");
        foreach (var card in result.Cards)
        {
            csv.Append(Csv(Suffix(card.CardId))).Append(',')
                .Append(Csv(card.Client.UserName)).Append(',')
                .Append(Csv(card.Business.BusinessName)).Append(',')
                .Append(card.CurrentStamps).Append(',')
                .Append(card.LifetimeStamps).Append(',')
                .Append(card.GoogleIssued || card.AppleTracked).Append(',')
                .Append(card.AppleRegisteredDeviceCount).Append(',')
                .Append(card.WalletIssueCount).Append(',')
                .Append(Csv(card.LastStampedAt.ToString("o")))
                .AppendLine();
        }

        var fileName = $"support-diagnostic-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.csv";
        _logger.LogInformation(
            "Admin {AdminUserId} exported support diagnostics CSV with query length {QueryLength}.",
            AdminAuth.GetAdminUserId(User),
            Query?.Trim().Length ?? 0);

        await _adminApp.RecordSupportExportAsync(
            new RecordSupportExportAuditCommand(
                AdminAuth.GetAdminUserId(User),
                "csv",
                Query ?? string.Empty,
                result.Cards.Count),
            cancellationToken);

        return File(
            Encoding.UTF8.GetBytes(csv.ToString()),
            "text/csv",
            fileName);
    }

    public async Task OnPostRetryWalletUpdateAsync(CancellationToken cancellationToken)
    {
        var result = await _adminApp.RetryWalletUpdateAsync(
            new AdminWalletRetryCommand(RetryCardId, AdminAuth.GetAdminUserId(User)),
            cancellationToken);

        if (result.Succeeded)
        {
            StatusMessage = "Reintento Wallet ejecutado. Revisa el evento AdminRetry.";
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "No se pudo ejecutar el reintento Wallet.";
        }

        if (HasSupportCriteria())
        {
            Result = await _adminApp.SearchSupportAsync(CreateSupportQuery(), cancellationToken);
        }
        else if (result.Card is not null)
        {
            Result = new AdminSupportResult(string.Empty, [], [], [result.Card]);
        }

        await LoadAuditAsync(cancellationToken);
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

    private async Task LoadAuditAsync(CancellationToken cancellationToken)
    {
        AuditEvents = await _adminApp.SearchAuditAsync(
            new AdminAuditQuery(AuditEventType, AuditSearch, AuditFrom, AuditTo, Limit: 100),
            cancellationToken);
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

    private static string SourceLabel(StampLedgerSource source)
    {
        return source == StampLedgerSource.LegacySync
            ? "Operacion externa"
            : source.ToString();
    }
}
