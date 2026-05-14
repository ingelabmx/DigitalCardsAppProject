using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;
using MySqlConnector;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlCutoverSmokeRepository : ICutoverSmokeRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlCutoverSmokeRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(CutoverSmokeEvidence evidence, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into ModernCutoverSmoke (
                BusinessID,
                AdminUserID,
                HealthOk,
                ReadyOk,
                EmailOk,
                AppleWalletOk,
                GoogleWalletOk,
                ModernStampOk,
                SupportReviewed,
                Notes,
                CreatedAt)
            values (
                @BusinessID,
                @AdminUserID,
                @HealthOk,
                @ReadyOk,
                @EmailOk,
                @AppleWalletOk,
                @GoogleWalletOk,
                @ModernStampOk,
                @SupportReviewed,
                @Notes,
                @CreatedAt);
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            await connection.ExecuteAsync(new CommandDefinition(
                sql,
                ToParameters(evidence),
                cancellationToken: cancellationToken));
        }
        catch (MySqlException exception) when (exception.Number == 1146)
        {
            throw MissingTableException(exception);
        }
    }

    public async Task<IReadOnlyList<CutoverSmokeEvidence>> ListRecentByBusinessIdAsync(
        Guid businessId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select ID,
                   BusinessID,
                   AdminUserID,
                   HealthOk,
                   ReadyOk,
                   EmailOk,
                   AppleWalletOk,
                   GoogleWalletOk,
                   ModernStampOk,
                   SupportReviewed,
                   Notes,
                   CreatedAt
            from ModernCutoverSmoke
            where BusinessID = @BusinessID
            order by CreatedAt desc, ID desc
            limit @Limit;
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            var rows = await connection.QueryAsync<CutoverSmokeRow>(new CommandDefinition(
                sql,
                new
                {
                    BusinessID = LegacyIdMapper.ToInt32(businessId),
                    Limit = Math.Max(1, Math.Min(limit, 25))
                },
                cancellationToken: cancellationToken));

            return rows.Select(row => row.ToDomain()).ToArray();
        }
        catch (MySqlException exception) when (exception.Number == 1146)
        {
            throw MissingTableException(exception);
        }
    }

    private static object ToParameters(CutoverSmokeEvidence evidence)
    {
        return new
        {
            BusinessID = LegacyIdMapper.ToInt32(evidence.BusinessId),
            AdminUserID = LegacyIdMapper.ToInt32(evidence.AdminUserId),
            evidence.HealthOk,
            evidence.ReadyOk,
            evidence.EmailOk,
            evidence.AppleWalletOk,
            evidence.GoogleWalletOk,
            evidence.ModernStampOk,
            evidence.SupportReviewed,
            evidence.Notes,
            CreatedAt = evidence.CreatedAt.UtcDateTime
        };
    }

    private static InvalidOperationException MissingTableException(MySqlException exception)
    {
        return new InvalidOperationException(
            "ModernCutoverSmoke table is missing. Run docs/migration-context/73-cutover-smoke-evidence-hostgator.sql before using cutover smoke evidence with MySQL.",
            exception);
    }

    private static DateTimeOffset AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc))
            : new DateTimeOffset(value.ToUniversalTime());
    }

    private sealed class CutoverSmokeRow
    {
        public long ID { get; set; }

        public int BusinessID { get; set; }

        public int AdminUserID { get; set; }

        public bool HealthOk { get; set; }

        public bool ReadyOk { get; set; }

        public bool EmailOk { get; set; }

        public bool AppleWalletOk { get; set; }

        public bool GoogleWalletOk { get; set; }

        public bool ModernStampOk { get; set; }

        public bool SupportReviewed { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }

        public CutoverSmokeEvidence ToDomain()
        {
            return new CutoverSmokeEvidence(
                ID,
                LegacyIdMapper.ToGuid(BusinessID),
                LegacyIdMapper.ToGuid(AdminUserID),
                HealthOk,
                ReadyOk,
                EmailOk,
                AppleWalletOk,
                GoogleWalletOk,
                ModernStampOk,
                SupportReviewed,
                Notes,
                AsUtc(CreatedAt));
        }
    }
}
