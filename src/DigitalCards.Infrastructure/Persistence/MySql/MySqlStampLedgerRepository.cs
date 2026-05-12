using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlStampLedgerRepository : IStampLedgerRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlStampLedgerRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(StampLedgerRecord record, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into StampLedger (
                CardID,
                BusinessID,
                UserID,
                Source,
                ActorBusinessID,
                PreviousCheckQTY,
                NewCheckQTY,
                PreviousHistoricCheckQTY,
                NewHistoricCheckQTY,
                ObservedLastCheck,
                GoogleWalletAttempted,
                GoogleWalletSucceeded,
                AppleWalletAttempted,
                AppleWalletSucceeded,
                ErrorSummary,
                CreatedAt)
            values (
                @CardID,
                @BusinessID,
                @UserID,
                @Source,
                @ActorBusinessID,
                @PreviousCheckQTY,
                @NewCheckQTY,
                @PreviousHistoricCheckQTY,
                @NewHistoricCheckQTY,
                @ObservedLastCheck,
                @GoogleWalletAttempted,
                @GoogleWalletSucceeded,
                @AppleWalletAttempted,
                @AppleWalletSucceeded,
                @ErrorSummary,
                @CreatedAt);
            """;

        await using var connection = _connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            ToParameters(record),
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<StampLedgerRecord>> ListRecentByCardIdAsync(
        Guid cardId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select ID,
                   CardID,
                   BusinessID,
                   UserID,
                   Source,
                   ActorBusinessID,
                   PreviousCheckQTY,
                   NewCheckQTY,
                   PreviousHistoricCheckQTY,
                   NewHistoricCheckQTY,
                   ObservedLastCheck,
                   GoogleWalletAttempted,
                   GoogleWalletSucceeded,
                   AppleWalletAttempted,
                   AppleWalletSucceeded,
                   ErrorSummary,
                   CreatedAt
            from StampLedger
            where CardID = @CardID
            order by CreatedAt desc, ID desc
            limit @Limit;
            """;

        var legacyCardId = LegacyIdMapper.TryGuidToInt32(cardId);
        if (legacyCardId is null)
        {
            return [];
        }

        await using var connection = _connectionFactory.Create();
        var rows = await connection.QueryAsync<StampLedgerRow>(new CommandDefinition(
            sql,
            new
            {
                CardID = legacyCardId.Value,
                Limit = Math.Max(1, Math.Min(limit, 50))
            },
            cancellationToken: cancellationToken));

        return rows.Select(row => row.ToModel()).ToArray();
    }

    private static object ToParameters(StampLedgerRecord record)
    {
        return new
        {
            CardID = LegacyIdMapper.ToInt32(record.CardId),
            BusinessID = LegacyIdMapper.ToInt32(record.BusinessId),
            UserID = LegacyIdMapper.ToInt32(record.UserId),
            Source = record.Source.ToString(),
            ActorBusinessID = record.ActorBusinessId is null
                ? (int?)null
                : LegacyIdMapper.ToInt32(record.ActorBusinessId.Value),
            record.PreviousCheckQTY,
            record.NewCheckQTY,
            record.PreviousHistoricCheckQTY,
            record.NewHistoricCheckQTY,
            ObservedLastCheck = record.ObservedLastCheck.UtcDateTime,
            record.GoogleWalletAttempted,
            record.GoogleWalletSucceeded,
            record.AppleWalletAttempted,
            record.AppleWalletSucceeded,
            record.ErrorSummary,
            CreatedAt = record.CreatedAt.UtcDateTime
        };
    }

    private static DateTimeOffset AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc))
            : new DateTimeOffset(value.ToUniversalTime());
    }

    private sealed class StampLedgerRow
    {
        public int ID { get; set; }

        public int CardID { get; set; }

        public int BusinessID { get; set; }

        public int UserID { get; set; }

        public string Source { get; set; } = string.Empty;

        public int? ActorBusinessID { get; set; }

        public int PreviousCheckQTY { get; set; }

        public int NewCheckQTY { get; set; }

        public int PreviousHistoricCheckQTY { get; set; }

        public int NewHistoricCheckQTY { get; set; }

        public DateTime ObservedLastCheck { get; set; }

        public bool GoogleWalletAttempted { get; set; }

        public bool GoogleWalletSucceeded { get; set; }

        public bool AppleWalletAttempted { get; set; }

        public bool AppleWalletSucceeded { get; set; }

        public string? ErrorSummary { get; set; }

        public DateTime CreatedAt { get; set; }

        public StampLedgerRecord ToModel()
        {
            return new StampLedgerRecord(
                ID,
                LegacyIdMapper.ToGuid(CardID),
                LegacyIdMapper.ToGuid(BusinessID),
                LegacyIdMapper.ToGuid(UserID),
                Enum.TryParse<StampLedgerSource>(Source, ignoreCase: true, out var source)
                    ? source
                    : StampLedgerSource.ModernBusiness,
                ActorBusinessID is null ? null : LegacyIdMapper.ToGuid(ActorBusinessID.Value),
                PreviousCheckQTY,
                NewCheckQTY,
                PreviousHistoricCheckQTY,
                NewHistoricCheckQTY,
                AsUtc(ObservedLastCheck),
                GoogleWalletAttempted,
                GoogleWalletSucceeded,
                AppleWalletAttempted,
                AppleWalletSucceeded,
                ErrorSummary,
                AsUtc(CreatedAt));
        }
    }
}
