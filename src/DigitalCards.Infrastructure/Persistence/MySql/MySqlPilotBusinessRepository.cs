using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;
using MySqlConnector;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlPilotBusinessRepository : IPilotBusinessRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlPilotBusinessRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PilotBusinessAccess?> FindByBusinessIdAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select BusinessID,
                   IsEnabled,
                   ActivationStatus,
                   Notes,
                   CreatedAt,
                   UpdatedAt,
                   UpdatedByAdminUserID
            from ModernPilotBusiness
            where BusinessID = @BusinessID
            limit 1;
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            var row = await connection.QuerySingleOrDefaultAsync<PilotBusinessRow>(
                new CommandDefinition(
                    sql,
                    new { BusinessID = LegacyIdMapper.ToInt32(businessId) },
                    cancellationToken: cancellationToken));

            return row?.ToDomain();
        }
        catch (MySqlException exception) when (exception.Number == 1146)
        {
            throw MissingTableException(exception);
        }
        catch (MySqlException exception) when (exception.Number == 1054)
        {
            throw MissingSchemaException(exception);
        }
    }

    public async Task<IReadOnlyList<PilotBusinessAccess>> ListAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            select BusinessID,
                   IsEnabled,
                   ActivationStatus,
                   Notes,
                   CreatedAt,
                   UpdatedAt,
                   UpdatedByAdminUserID
            from ModernPilotBusiness;
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            var rows = await connection.QueryAsync<PilotBusinessRow>(
                new CommandDefinition(sql, cancellationToken: cancellationToken));

            return rows.Select(row => row.ToDomain()).ToArray();
        }
        catch (MySqlException exception) when (exception.Number == 1146)
        {
            throw MissingTableException(exception);
        }
        catch (MySqlException exception) when (exception.Number == 1054)
        {
            throw MissingSchemaException(exception);
        }
    }

    public async Task UpsertAsync(PilotBusinessAccess access, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into ModernPilotBusiness (
                BusinessID,
                IsEnabled,
                ActivationStatus,
                Notes,
                CreatedAt,
                UpdatedAt,
                UpdatedByAdminUserID)
            values (
                @BusinessID,
                @IsEnabled,
                @ActivationStatus,
                @Notes,
                @CreatedAt,
                @UpdatedAt,
                @UpdatedByAdminUserID)
            on duplicate key update
                IsEnabled = values(IsEnabled),
                ActivationStatus = values(ActivationStatus),
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
                    BusinessID = LegacyIdMapper.ToInt32(access.BusinessId),
                    access.IsEnabled,
                    ActivationStatus = access.ActivationStatus.ToString(),
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
        catch (MySqlException exception) when (exception.Number == 1054)
        {
            throw MissingSchemaException(exception);
        }
    }

    private static InvalidOperationException MissingTableException(MySqlException exception)
    {
        return new InvalidOperationException(
            "ModernPilotBusiness table is missing. Run docs/migration-context/22-admin-pilot-management-hostgator.sql before using admin pilot management with MySQL.",
            exception);
    }

    private static InvalidOperationException MissingSchemaException(MySqlException exception)
    {
        return new InvalidOperationException(
            "ModernPilotBusiness activation status schema is missing. Run docs/migration-context/38-admin-business-activation-status-hostgator.sql before using business activation status with MySQL.",
            exception);
    }

    private static DateTimeOffset AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc))
            : new DateTimeOffset(value.ToUniversalTime());
    }

    private sealed record PilotBusinessRow(
        int BusinessID,
        bool IsEnabled,
        string? ActivationStatus,
        string? Notes,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int UpdatedByAdminUserID)
    {
        public PilotBusinessAccess ToDomain()
        {
            return new PilotBusinessAccess(
                LegacyIdMapper.ToGuid(BusinessID),
                IsEnabled,
                Notes,
                AsUtc(CreatedAt),
                AsUtc(UpdatedAt),
                LegacyIdMapper.ToGuid(UpdatedByAdminUserID),
                ParseActivationStatus(ActivationStatus, IsEnabled));
        }

        private static BusinessActivationStatus ParseActivationStatus(string? value, bool isEnabled)
        {
            return Enum.TryParse<BusinessActivationStatus>(value, ignoreCase: true, out var status)
                ? status
                : isEnabled
                    ? BusinessActivationStatus.ModernPrimary
                    : BusinessActivationStatus.Inactive;
        }
    }
}
