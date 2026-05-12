using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;
using MySqlConnector;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlBusinessCredentialRepository : IBusinessCredentialRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlBusinessCredentialRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<BusinessCredential?> FindByBusinessIdAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select BusinessID,
                   PasswordHash,
                   CreatedAt,
                   UpdatedAt
            from ModernBusinessCredential
            where BusinessID = @BusinessId
            limit 1;
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            var row = await connection.QuerySingleOrDefaultAsync<BusinessCredentialRow>(
                new CommandDefinition(
                    sql,
                    new { BusinessId = LegacyIdMapper.ToInt32(businessId) },
                    cancellationToken: cancellationToken));

            return row?.ToDomain();
        }
        catch (MySqlException ex) when (ex.Number == 1146)
        {
            throw MissingTableException(ex);
        }
    }

    public async Task UpsertAsync(
        BusinessCredential credential,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into ModernBusinessCredential (BusinessID, PasswordHash, CreatedAt, UpdatedAt)
            values (@BusinessId, @PasswordHash, @CreatedAt, @UpdatedAt)
            on duplicate key update
                PasswordHash = values(PasswordHash),
                UpdatedAt = values(UpdatedAt);
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    BusinessId = LegacyIdMapper.ToInt32(credential.BusinessId),
                    credential.PasswordHash,
                    CreatedAt = credential.CreatedAt.UtcDateTime,
                    UpdatedAt = credential.UpdatedAt.UtcDateTime
                },
                cancellationToken: cancellationToken));
        }
        catch (MySqlException ex) when (ex.Number == 1146)
        {
            throw MissingTableException(ex);
        }
    }

    private static InvalidOperationException MissingTableException(MySqlException exception)
    {
        return new InvalidOperationException(
            "ModernBusinessCredential table is missing. Run docs/migration-context/16-business-password-hardening-hostgator.sql before using MySQL business login.",
            exception);
    }

    private sealed record BusinessCredentialRow(
        int BusinessID,
        string PasswordHash,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        public BusinessCredential ToDomain()
        {
            return new BusinessCredential(
                LegacyIdMapper.ToGuid(BusinessID),
                PasswordHash,
                new DateTimeOffset(DateTime.SpecifyKind(CreatedAt, DateTimeKind.Utc)),
                new DateTimeOffset(DateTime.SpecifyKind(UpdatedAt, DateTimeKind.Utc)));
        }
    }
}
