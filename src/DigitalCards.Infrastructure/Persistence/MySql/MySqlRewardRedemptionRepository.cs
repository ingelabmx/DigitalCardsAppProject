using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using MySqlConnector;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlRewardRedemptionRepository : IRewardRedemptionRepository
{
    private const int MissingTableErrorNumber = 1146;

    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlRewardRedemptionRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(RewardRedemptionRecord record, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into RewardRedemption (
                CardID,
                BusinessID,
                UserID,
                ActorBusinessID,
                StampGoal,
                RedeemedCheckQTY,
                HistoricCheckQTY,
                RewardText,
                GoogleWalletAttempted,
                GoogleWalletSucceeded,
                AppleWalletAttempted,
                AppleWalletSucceeded,
                ErrorSummary,
                RedeemedAt,
                CreatedAt)
            values (
                @CardID,
                @BusinessID,
                @UserID,
                @ActorBusinessID,
                @StampGoal,
                @RedeemedCheckQTY,
                @HistoricCheckQTY,
                @RewardText,
                @GoogleWalletAttempted,
                @GoogleWalletSucceeded,
                @AppleWalletAttempted,
                @AppleWalletSucceeded,
                @ErrorSummary,
                @RedeemedAt,
                @CreatedAt);
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            await connection.ExecuteAsync(new CommandDefinition(
                sql,
                ToParameters(record),
                cancellationToken: cancellationToken));
        }
        catch (MySqlException exception) when (exception.Number == MissingTableErrorNumber)
        {
            throw new InvalidOperationException(
                "RewardRedemption table is missing. Run docs/migration-context/118-reward-redemption-cycles-hostgator.sql before redeeming rewards.",
                exception);
        }
    }

    public async Task<IReadOnlyList<RewardRedemptionRecord>> ListRecentByCardIdAsync(
        Guid cardId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select ID,
                   CardID,
                   BusinessID,
                   UserID,
                   ActorBusinessID,
                   StampGoal,
                   RedeemedCheckQTY,
                   HistoricCheckQTY,
                   RewardText,
                   GoogleWalletAttempted,
                   GoogleWalletSucceeded,
                   AppleWalletAttempted,
                   AppleWalletSucceeded,
                   ErrorSummary,
                   RedeemedAt,
                   CreatedAt
            from RewardRedemption
            where CardID = @CardID
            order by RedeemedAt desc, ID desc
            limit @Limit;
            """;

        var legacyCardId = LegacyIdMapper.TryGuidToInt32(cardId);
        if (legacyCardId is null)
        {
            return [];
        }

        IEnumerable<RewardRedemptionRow> rows;
        try
        {
            await using var connection = _connectionFactory.Create();
            rows = await connection.QueryAsync<RewardRedemptionRow>(new CommandDefinition(
                sql,
                new
                {
                    CardID = legacyCardId.Value,
                    Limit = Math.Max(1, Math.Min(limit, 50))
                },
                cancellationToken: cancellationToken));
        }
        catch (MySqlException exception) when (exception.Number == MissingTableErrorNumber)
        {
            return [];
        }

        return rows.Select(row => row.ToModel()).ToArray();
    }

    private static object ToParameters(RewardRedemptionRecord record)
    {
        return new
        {
            CardID = LegacyIdMapper.ToInt32(record.CardId),
            BusinessID = LegacyIdMapper.ToInt32(record.BusinessId),
            UserID = LegacyIdMapper.ToInt32(record.UserId),
            ActorBusinessID = record.ActorBusinessId is null
                ? (int?)null
                : LegacyIdMapper.ToInt32(record.ActorBusinessId.Value),
            record.StampGoal,
            record.RedeemedCheckQTY,
            record.HistoricCheckQTY,
            RewardText = SafeRewardText(record.RewardText),
            record.GoogleWalletAttempted,
            record.GoogleWalletSucceeded,
            record.AppleWalletAttempted,
            record.AppleWalletSucceeded,
            record.ErrorSummary,
            RedeemedAt = record.RedeemedAt.UtcDateTime,
            CreatedAt = record.CreatedAt.UtcDateTime
        };
    }

    private static string SafeRewardText(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= 280 ? trimmed : trimmed[..280];
    }

    private static DateTimeOffset AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc))
            : new DateTimeOffset(value.ToUniversalTime());
    }

    private sealed class RewardRedemptionRow
    {
        public long ID { get; set; }

        public int CardID { get; set; }

        public int BusinessID { get; set; }

        public int UserID { get; set; }

        public int? ActorBusinessID { get; set; }

        public int StampGoal { get; set; }

        public int RedeemedCheckQTY { get; set; }

        public int HistoricCheckQTY { get; set; }

        public string RewardText { get; set; } = string.Empty;

        public bool GoogleWalletAttempted { get; set; }

        public bool GoogleWalletSucceeded { get; set; }

        public bool AppleWalletAttempted { get; set; }

        public bool AppleWalletSucceeded { get; set; }

        public string? ErrorSummary { get; set; }

        public DateTime RedeemedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public RewardRedemptionRecord ToModel()
        {
            return new RewardRedemptionRecord(
                ID,
                LegacyIdMapper.ToGuid(CardID),
                LegacyIdMapper.ToGuid(BusinessID),
                LegacyIdMapper.ToGuid(UserID),
                ActorBusinessID is null ? null : LegacyIdMapper.ToGuid(ActorBusinessID.Value),
                StampGoal,
                RedeemedCheckQTY,
                HistoricCheckQTY,
                RewardText,
                GoogleWalletAttempted,
                GoogleWalletSucceeded,
                AppleWalletAttempted,
                AppleWalletSucceeded,
                ErrorSummary,
                AsUtc(RedeemedAt),
                AsUtc(CreatedAt));
        }
    }
}
