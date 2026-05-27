using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlBusinessEnrollmentLinkRepository : IBusinessEnrollmentLinkRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlBusinessEnrollmentLinkRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(BusinessEnrollmentLinkRecord token, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into BusinessEnrollmentLinkToken (TokenHash, TokenSuffix, Token, BusinessID, CreatedAt, LastUsedAt, RevokedAt)
            values (@TokenHash, @TokenSuffix, @Token, @BusinessID, @CreatedAt, @LastUsedAt, @RevokedAt);
            """;

        await using var connection = _connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParameters(token), cancellationToken: cancellationToken));
    }

    public async Task<BusinessEnrollmentLinkRecord?> FindActiveByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select TokenHash,
                   TokenSuffix,
                   Token,
                   BusinessID,
                   CreatedAt,
                   LastUsedAt,
                   RevokedAt
            from BusinessEnrollmentLinkToken
            where TokenHash = @TokenHash
              and RevokedAt is null
            limit 1;
            """;

        await using var connection = _connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<BusinessEnrollmentLinkTokenRow>(
            new CommandDefinition(sql, new { TokenHash = tokenHash }, cancellationToken: cancellationToken));

        return row?.ToModel();
    }

    public async Task<IReadOnlyList<BusinessEnrollmentLinkRecord>> ListActiveByBusinessIdAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select TokenHash,
                   TokenSuffix,
                   Token,
                   BusinessID,
                   CreatedAt,
                   LastUsedAt,
                   RevokedAt
            from BusinessEnrollmentLinkToken
            where BusinessID = @BusinessID
              and RevokedAt is null
            order by CreatedAt desc;
            """;

        var legacyBusinessId = LegacyIdMapper.TryGuidToInt32(businessId);
        if (legacyBusinessId is null)
        {
            return [];
        }

        await using var connection = _connectionFactory.Create();
        var rows = await connection.QueryAsync<BusinessEnrollmentLinkTokenRow>(
            new CommandDefinition(sql, new { BusinessID = legacyBusinessId.Value }, cancellationToken: cancellationToken));

        return rows.Select(row => row.ToModel()).ToArray();
    }

    public async Task MarkUsedAsync(
        string tokenHash,
        DateTimeOffset usedAt,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            update BusinessEnrollmentLinkToken
            set LastUsedAt = @LastUsedAt
            where TokenHash = @TokenHash
              and RevokedAt is null;
            """;

        await using var connection = _connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            TokenHash = tokenHash,
            LastUsedAt = usedAt.UtcDateTime
        }, cancellationToken: cancellationToken));
    }

    public async Task RevokeActiveByBusinessIdAsync(
        Guid businessId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            update BusinessEnrollmentLinkToken
            set RevokedAt = @RevokedAt
            where BusinessID = @BusinessID
              and RevokedAt is null;
            """;

        var legacyBusinessId = LegacyIdMapper.TryGuidToInt32(businessId);
        if (legacyBusinessId is null)
        {
            return;
        }

        await using var connection = _connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            BusinessID = legacyBusinessId.Value,
            RevokedAt = revokedAt.UtcDateTime
        }, cancellationToken: cancellationToken));
    }

    private static object ToParameters(BusinessEnrollmentLinkRecord token)
    {
        return new
        {
            token.TokenHash,
            token.TokenSuffix,
            token.Token,
            BusinessID = LegacyIdMapper.ToInt32(token.BusinessId),
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

    private sealed record BusinessEnrollmentLinkTokenRow(
        string TokenHash,
        string TokenSuffix,
        string Token,
        int BusinessID,
        DateTime CreatedAt,
        DateTime? LastUsedAt,
        DateTime? RevokedAt)
    {
        public BusinessEnrollmentLinkRecord ToModel()
        {
            return new BusinessEnrollmentLinkRecord(
                LegacyIdMapper.ToGuid(BusinessID),
                TokenHash,
                TokenSuffix,
                Token,
                AsUtc(CreatedAt),
                LastUsedAt is null ? null : AsUtc(LastUsedAt.Value),
                RevokedAt is null ? null : AsUtc(RevokedAt.Value));
        }
    }
}
