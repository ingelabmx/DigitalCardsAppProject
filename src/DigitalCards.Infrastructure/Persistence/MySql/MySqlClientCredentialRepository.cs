using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;
using MySqlConnector;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlClientCredentialRepository : IClientCredentialRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlClientCredentialRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ClientCredential?> FindByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select UserID,
                   PasswordHash,
                   CreatedAt,
                   UpdatedAt
            from ModernClientCredential
            where UserID = @UserId
            limit 1;
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            var row = await connection.QuerySingleOrDefaultAsync<ClientCredentialRow>(
                new CommandDefinition(
                    sql,
                    new { UserId = LegacyIdMapper.ToInt32(clientId) },
                    cancellationToken: cancellationToken));

            return row?.ToDomain();
        }
        catch (MySqlException ex) when (ex.Number == 1146)
        {
            throw MissingTableException(ex);
        }
    }

    public async Task UpsertAsync(
        ClientCredential credential,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into ModernClientCredential (UserID, PasswordHash, CreatedAt, UpdatedAt)
            values (@UserId, @PasswordHash, @CreatedAt, @UpdatedAt)
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
                    UserId = LegacyIdMapper.ToInt32(credential.ClientId),
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
            "ModernClientCredential table is missing. Run docs/migration-context/28-client-password-hardening-hostgator.sql before using MySQL client login.",
            exception);
    }

    private sealed record ClientCredentialRow(
        int UserID,
        string PasswordHash,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        public ClientCredential ToDomain()
        {
            return new ClientCredential(
                LegacyIdMapper.ToGuid(UserID),
                PasswordHash,
                new DateTimeOffset(DateTime.SpecifyKind(CreatedAt, DateTimeKind.Utc)),
                new DateTimeOffset(DateTime.SpecifyKind(UpdatedAt, DateTimeKind.Utc)));
        }
    }
}
