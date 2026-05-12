using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlWalletLinkTokenRepository : IWalletLinkTokenRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlWalletLinkTokenRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(WalletLinkTokenRecord token, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into WalletLinkToken (TokenHash, TokenSuffix, CardID, Purpose, CreatedAt, LastUsedAt, RevokedAt)
            values (@TokenHash, @TokenSuffix, @CardID, @Purpose, @CreatedAt, @LastUsedAt, @RevokedAt);
            """;

        await using var connection = _connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParameters(token), cancellationToken: cancellationToken));
    }

    public async Task<WalletLinkTokenRecord?> FindActiveByTokenHashAsync(
        string tokenHash,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select TokenHash,
                   TokenSuffix,
                   CardID,
                   Purpose,
                   CreatedAt,
                   LastUsedAt,
                   RevokedAt
            from WalletLinkToken
            where TokenHash = @TokenHash
              and Purpose = @Purpose
              and RevokedAt is null
            limit 1;
            """;

        await using var connection = _connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<WalletLinkTokenRow>(
            new CommandDefinition(sql, new { TokenHash = tokenHash, Purpose = purpose }, cancellationToken: cancellationToken));

        return row?.ToModel();
    }

    public async Task<IReadOnlyList<WalletLinkTokenRecord>> ListActiveByCardIdAsync(
        Guid cardId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select TokenHash,
                   TokenSuffix,
                   CardID,
                   Purpose,
                   CreatedAt,
                   LastUsedAt,
                   RevokedAt
            from WalletLinkToken
            where CardID = @CardID
              and Purpose = @Purpose
              and RevokedAt is null
            order by CreatedAt desc;
            """;

        var legacyCardId = LegacyIdMapper.TryGuidToInt32(cardId);
        if (legacyCardId is null)
        {
            return [];
        }

        await using var connection = _connectionFactory.Create();
        var rows = await connection.QueryAsync<WalletLinkTokenRow>(
            new CommandDefinition(sql, new { CardID = legacyCardId.Value, Purpose = purpose }, cancellationToken: cancellationToken));

        return rows.Select(row => row.ToModel()).ToArray();
    }

    public async Task MarkUsedAsync(
        string tokenHash,
        string purpose,
        DateTimeOffset usedAt,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            update WalletLinkToken
            set LastUsedAt = @LastUsedAt
            where TokenHash = @TokenHash
              and Purpose = @Purpose
              and RevokedAt is null;
            """;

        await using var connection = _connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            TokenHash = tokenHash,
            Purpose = purpose,
            LastUsedAt = usedAt.UtcDateTime
        }, cancellationToken: cancellationToken));
    }

    public async Task RevokeActiveByCardIdAsync(
        Guid cardId,
        string purpose,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            update WalletLinkToken
            set RevokedAt = @RevokedAt
            where CardID = @CardID
              and Purpose = @Purpose
              and RevokedAt is null;
            """;

        var legacyCardId = LegacyIdMapper.TryGuidToInt32(cardId);
        if (legacyCardId is null)
        {
            return;
        }

        await using var connection = _connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            CardID = legacyCardId.Value,
            Purpose = purpose,
            RevokedAt = revokedAt.UtcDateTime
        }, cancellationToken: cancellationToken));
    }

    private static object ToParameters(WalletLinkTokenRecord token)
    {
        return new
        {
            token.TokenHash,
            token.TokenSuffix,
            CardID = LegacyIdMapper.ToInt32(token.CardId),
            token.Purpose,
            CreatedAt = token.CreatedAt.UtcDateTime,
            LastUsedAt = token.LastUsedAt?.UtcDateTime,
            RevokedAt = token.RevokedAt?.UtcDateTime
        };
    }

    private static DateTimeOffset AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc))
            : new DateTimeOffset(value.ToUniversalTime());
    }

    private sealed record WalletLinkTokenRow(
        string TokenHash,
        string TokenSuffix,
        int CardID,
        string Purpose,
        DateTime CreatedAt,
        DateTime? LastUsedAt,
        DateTime? RevokedAt)
    {
        public WalletLinkTokenRecord ToModel()
        {
            return new WalletLinkTokenRecord(
                LegacyIdMapper.ToGuid(CardID),
                Purpose,
                TokenHash,
                TokenSuffix,
                AsUtc(CreatedAt),
                LastUsedAt is null ? null : AsUtc(LastUsedAt.Value),
                RevokedAt is null ? null : AsUtc(RevokedAt.Value));
        }
    }
}
