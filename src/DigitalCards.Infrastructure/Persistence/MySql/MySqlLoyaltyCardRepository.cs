using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlLoyaltyCardRepository : ILoyaltyCardRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlLoyaltyCardRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<LoyaltyCard> AddAsync(LoyaltyCard card, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into ClientCard (CardIDGoogle, CreationDate, CheckQTY, LastCheck, UserID, BusinessID, HistoricCheckQTY)
            values (@GoogleObjectId, @CreatedAt, @CurrentStamps, @LastStampedAt, @ClientId, @BusinessId, @HistoricCheckQTY);
            select last_insert_id();
            """;

        await using var connection = _connectionFactory.Create();
        var cardId = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, ToInsertParameters(card), cancellationToken: cancellationToken));

        return await FindByIdAsync(cardId, cancellationToken)
            ?? throw new InvalidOperationException("Inserted legacy card could not be loaded.");
    }

    public async Task UpdateAsync(LoyaltyCard card, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update ClientCard
            set CheckQTY = @CurrentStamps,
                HistoricCheckQTY = @HistoricCheckQTY,
                LastCheck = @LastStampedAt,
                CardIDGoogle = @GoogleObjectId
            where CardID = @Id;
            """;

        await using var connection = _connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, ToUpdateParameters(card), cancellationToken: cancellationToken));
    }

    public async Task<LoyaltyCard?> FindByClientAndBusinessAsync(
        Guid clientId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select CardID,
                   CardIDGoogle,
                   CreationDate,
                   CheckQTY,
                   LastCheck,
                   UserID,
                   BusinessID,
                   HistoricCheckQTY
            from ClientCard
            where UserID = @ClientId
              and BusinessID = @BusinessId
            limit 1;
            """;

        await using var connection = _connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<LoyaltyCardRow>(
            new CommandDefinition(sql, new
            {
                ClientId = LegacyIdMapper.ToInt32(clientId),
                BusinessId = LegacyIdMapper.ToInt32(businessId)
            }, cancellationToken: cancellationToken));

        return row?.ToDomain();
    }

    public async Task<LoyaltyCard?> FindByEnrollmentTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select CardID,
                   CardIDGoogle,
                   CreationDate,
                   CheckQTY,
                   LastCheck,
                   UserID,
                   BusinessID,
                   HistoricCheckQTY
            from ClientCard
            where CardID = @CardId
            limit 1;
            """;

        var cardId = LegacyIdMapper.TryTokenToInt32(token);
        if (cardId is null)
        {
            return null;
        }

        await using var connection = _connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<LoyaltyCardRow>(
            new CommandDefinition(sql, new { CardId = cardId }, cancellationToken: cancellationToken));

        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<LoyaltyCard>> ListByClientAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select CardID,
                   CardIDGoogle,
                   CreationDate,
                   CheckQTY,
                   LastCheck,
                   UserID,
                   BusinessID,
                   HistoricCheckQTY
            from ClientCard
            where UserID = @ClientId
            order by CreationDate desc;
            """;

        await using var connection = _connectionFactory.Create();
        var rows = await connection.QueryAsync<LoyaltyCardRow>(
            new CommandDefinition(sql, new { ClientId = LegacyIdMapper.ToInt32(clientId) }, cancellationToken: cancellationToken));

        return rows.Select(row => row.ToDomain()).ToArray();
    }

    private async Task<LoyaltyCard?> FindByIdAsync(int cardId, CancellationToken cancellationToken)
    {
        const string sql = """
            select CardID,
                   CardIDGoogle,
                   CreationDate,
                   CheckQTY,
                   LastCheck,
                   UserID,
                   BusinessID,
                   HistoricCheckQTY
            from ClientCard
            where CardID = @CardId
            limit 1;
            """;

        await using var connection = _connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<LoyaltyCardRow>(
            new CommandDefinition(sql, new { CardId = cardId }, cancellationToken: cancellationToken));

        return row?.ToDomain();
    }

    private static object ToInsertParameters(LoyaltyCard card)
    {
        return new
        {
            ClientId = LegacyIdMapper.ToInt32(card.ClientId),
            BusinessId = LegacyIdMapper.ToInt32(card.BusinessId),
            card.CurrentStamps,
            HistoricCheckQTY = card.LifetimeStamps,
            CreatedAt = card.CreatedAt.UtcDateTime,
            LastStampedAt = card.LastStampedAt.UtcDateTime,
            GoogleObjectId = ToLegacyGoogleId(card.GoogleObjectId)
        };
    }

    private static object ToUpdateParameters(LoyaltyCard card)
    {
        return new
        {
            Id = LegacyIdMapper.ToInt32(card.Id),
            card.CurrentStamps,
            HistoricCheckQTY = card.LifetimeStamps,
            LastStampedAt = card.LastStampedAt.UtcDateTime,
            GoogleObjectId = ToLegacyGoogleId(card.GoogleObjectId)
        };
    }

    private static string? ToLegacyGoogleId(string? objectId)
    {
        if (string.IsNullOrWhiteSpace(objectId))
        {
            return null;
        }

        var normalized = new string(objectId.Where(char.IsLetterOrDigit).ToArray());
        if (normalized.Length <= 10)
        {
            return normalized;
        }

        return normalized[^10..];
    }

    private static DateTimeOffset AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc))
            : new DateTimeOffset(value.ToUniversalTime());
    }

    private sealed record LoyaltyCardRow(
        int CardID,
        string? CardIDGoogle,
        DateTime? CreationDate,
        int? CheckQTY,
        DateTime? LastCheck,
        int UserID,
        int BusinessID,
        int? HistoricCheckQTY)
    {
        public LoyaltyCard ToDomain()
        {
            var cardGuid = LegacyIdMapper.ToGuid(CardID);
            var createdAt = AsUtc(CreationDate ?? DateTime.UtcNow);
            var currentStamps = CheckQTY ?? 0;
            var lifetimeStamps = Math.Max(HistoricCheckQTY ?? currentStamps, currentStamps);
            return LoyaltyCard.Restore(
                cardGuid,
                LegacyIdMapper.ToGuid(UserID),
                LegacyIdMapper.ToGuid(BusinessID),
                cardGuid.ToString("N"),
                currentStamps,
                lifetimeStamps,
                createdAt,
                AsUtc(LastCheck ?? CreationDate ?? DateTime.UtcNow),
                string.IsNullOrWhiteSpace(CardIDGoogle) ? null : CardIDGoogle,
                googleSaveUrl: null);
        }
    }
}
