using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using MySqlConnector;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlPasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlPasswordResetTokenRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(
        PasswordResetTokenRecord token,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into ModernPasswordResetToken
                (AccountType, AccountID, TokenHash, TokenSuffix, CreatedAt, ExpiresAt, UsedAt, RevokedAt)
            values
                (@AccountType, @AccountID, @TokenHash, @TokenSuffix, @CreatedAt, @ExpiresAt, @UsedAt, @RevokedAt);
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            await connection.ExecuteAsync(new CommandDefinition(
                sql,
                ToParameters(token),
                cancellationToken: cancellationToken));
        }
        catch (MySqlException ex) when (ex.Number == 1146)
        {
            throw MissingTableException(ex);
        }
    }

    public async Task<PasswordResetTokenRecord?> FindActiveByTokenHashAsync(
        string tokenHash,
        PasswordResetAccountType accountType,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select ID,
                   AccountType,
                   AccountID,
                   TokenHash,
                   TokenSuffix,
                   CreatedAt,
                   ExpiresAt,
                   UsedAt,
                   RevokedAt
            from ModernPasswordResetToken
            where TokenHash = @TokenHash
              and AccountType = @AccountType
              and UsedAt is null
              and RevokedAt is null
              and ExpiresAt > @Now
            limit 1;
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            var row = await connection.QuerySingleOrDefaultAsync<PasswordResetTokenRow>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        TokenHash = tokenHash,
                        AccountType = accountType.ToString(),
                        Now = now.UtcDateTime
                    },
                    cancellationToken: cancellationToken));

            return row?.ToModel();
        }
        catch (MySqlException ex) when (ex.Number == 1146)
        {
            throw MissingTableException(ex);
        }
    }

    public async Task MarkUsedAsync(
        long id,
        DateTimeOffset usedAt,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            update ModernPasswordResetToken
            set UsedAt = @UsedAt
            where ID = @Id
              and UsedAt is null;
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new { Id = id, UsedAt = usedAt.UtcDateTime },
                cancellationToken: cancellationToken));
        }
        catch (MySqlException ex) when (ex.Number == 1146)
        {
            throw MissingTableException(ex);
        }
    }

    public async Task RevokeActiveByAccountAsync(
        PasswordResetAccountType accountType,
        Guid accountId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            update ModernPasswordResetToken
            set RevokedAt = @RevokedAt
            where AccountType = @AccountType
              and AccountID = @AccountID
              and UsedAt is null
              and RevokedAt is null;
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    AccountType = accountType.ToString(),
                    AccountID = LegacyIdMapper.ToInt32(accountId),
                    RevokedAt = revokedAt.UtcDateTime
                },
                cancellationToken: cancellationToken));
        }
        catch (MySqlException ex) when (ex.Number == 1146)
        {
            throw MissingTableException(ex);
        }
    }

    private static object ToParameters(PasswordResetTokenRecord token)
    {
        return new
        {
            AccountType = token.AccountType.ToString(),
            AccountID = LegacyIdMapper.ToInt32(token.AccountId),
            token.TokenHash,
            token.TokenSuffix,
            CreatedAt = token.CreatedAt.UtcDateTime,
            ExpiresAt = token.ExpiresAt.UtcDateTime,
            UsedAt = token.UsedAt?.UtcDateTime,
            RevokedAt = token.RevokedAt?.UtcDateTime
        };
    }

    private static InvalidOperationException MissingTableException(MySqlException exception)
    {
        return new InvalidOperationException(
            "ModernPasswordResetToken table is missing. Run docs/migration-context/33-password-reset-flows-v1-hostgator.sql before using MySQL password reset flows.",
            exception);
    }

    private sealed class PasswordResetTokenRow
    {
        public long ID { get; set; }

        public string AccountType { get; set; } = string.Empty;

        public int AccountID { get; set; }

        public string TokenHash { get; set; } = string.Empty;

        public string TokenSuffix { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        public PasswordResetTokenRecord ToModel()
        {
            return new PasswordResetTokenRecord(
                ID,
                Enum.Parse<PasswordResetAccountType>(AccountType, ignoreCase: true),
                LegacyIdMapper.ToGuid(AccountID),
                TokenHash,
                TokenSuffix,
                new DateTimeOffset(DateTime.SpecifyKind(CreatedAt, DateTimeKind.Utc)),
                new DateTimeOffset(DateTime.SpecifyKind(ExpiresAt, DateTimeKind.Utc)),
                UsedAt is null ? null : new DateTimeOffset(DateTime.SpecifyKind(UsedAt.Value, DateTimeKind.Utc)),
                RevokedAt is null ? null : new DateTimeOffset(DateTime.SpecifyKind(RevokedAt.Value, DateTimeKind.Utc)));
        }
    }
}
