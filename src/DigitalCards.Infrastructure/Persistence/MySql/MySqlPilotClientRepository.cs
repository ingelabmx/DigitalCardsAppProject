using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;
using MySqlConnector;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlPilotClientRepository : IPilotClientRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlPilotClientRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PilotClientAccess?> FindByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select UserID,
                   IsEnabled,
                   Notes,
                   CreatedAt,
                   UpdatedAt,
                   UpdatedByAdminUserID
            from ModernPilotClient
            where UserID = @UserID
            limit 1;
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            var row = await connection.QuerySingleOrDefaultAsync<PilotClientRow>(
                new CommandDefinition(
                    sql,
                    new { UserID = LegacyIdMapper.ToInt32(clientId) },
                    cancellationToken: cancellationToken));

            return row?.ToDomain();
        }
        catch (MySqlException exception) when (exception.Number == 1146)
        {
            throw MissingTableException(exception);
        }
    }

    public async Task<IReadOnlyList<PilotClientAccess>> ListAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            select UserID,
                   IsEnabled,
                   Notes,
                   CreatedAt,
                   UpdatedAt,
                   UpdatedByAdminUserID
            from ModernPilotClient;
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            var rows = await connection.QueryAsync<PilotClientRow>(
                new CommandDefinition(sql, cancellationToken: cancellationToken));

            return rows.Select(row => row.ToDomain()).ToArray();
        }
        catch (MySqlException exception) when (exception.Number == 1146)
        {
            throw MissingTableException(exception);
        }
    }

    public async Task UpsertAsync(PilotClientAccess access, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into ModernPilotClient (
                UserID,
                IsEnabled,
                Notes,
                CreatedAt,
                UpdatedAt,
                UpdatedByAdminUserID)
            values (
                @UserID,
                @IsEnabled,
                @Notes,
                @CreatedAt,
                @UpdatedAt,
                @UpdatedByAdminUserID)
            on duplicate key update
                IsEnabled = values(IsEnabled),
                Notes = values(Notes),
                UpdatedAt = values(UpdatedAt),
                UpdatedByAdminUserID = values(UpdatedByAdminUserID);
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    UserID = LegacyIdMapper.ToInt32(access.ClientId),
                    access.IsEnabled,
                    access.Notes,
                    CreatedAt = access.CreatedAt.UtcDateTime,
                    UpdatedAt = access.UpdatedAt.UtcDateTime,
                    UpdatedByAdminUserID = LegacyIdMapper.ToInt32(access.UpdatedByAdminUserId)
                },
                cancellationToken: cancellationToken));
        }
        catch (MySqlException exception) when (exception.Number == 1146)
        {
            throw MissingTableException(exception);
        }
    }

    private static InvalidOperationException MissingTableException(MySqlException exception)
    {
        return new InvalidOperationException(
            "ModernPilotClient table is missing. Run docs/migration-context/26-client-pilot-management-hostgator.sql before using admin client pilot management with MySQL.",
            exception);
    }

    private static DateTimeOffset AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc))
            : new DateTimeOffset(value.ToUniversalTime());
    }

    private sealed class PilotClientRow
    {
        public int UserID { get; set; }

        public bool IsEnabled { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public int UpdatedByAdminUserID { get; set; }

        public PilotClientAccess ToDomain()
        {
            return new PilotClientAccess(
                LegacyIdMapper.ToGuid(UserID),
                IsEnabled,
                Notes,
                AsUtc(CreatedAt),
                AsUtc(UpdatedAt),
                LegacyIdMapper.ToGuid(UpdatedByAdminUserID));
        }
    }
}
