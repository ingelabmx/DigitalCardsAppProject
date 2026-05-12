using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;
using MySqlConnector;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlAdminCredentialRepository : IAdminCredentialRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlAdminCredentialRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AdminCredential?> FindByAdminUserIdAsync(
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select UserID,
                   PasswordHash,
                   CreatedAt,
                   UpdatedAt
            from ModernAdminCredential
            where UserID = @UserId
            limit 1;
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            var row = await connection.QuerySingleOrDefaultAsync<AdminCredentialRow>(
                new CommandDefinition(
                    sql,
                    new { UserId = LegacyIdMapper.ToInt32(adminUserId) },
                    cancellationToken: cancellationToken));

            return row?.ToDomain();
        }
        catch (MySqlException ex) when (ex.Number == 1146)
        {
            throw MissingTableException(ex);
        }
    }

    public async Task UpsertAsync(
        AdminCredential credential,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into ModernAdminCredential (UserID, PasswordHash, CreatedAt, UpdatedAt)
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
                    UserId = LegacyIdMapper.ToInt32(credential.AdminUserId),
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
            "ModernAdminCredential table is missing. Run docs/migration-context/25-admin-access-management-hostgator.sql before using MySQL admin access management.",
            exception);
    }

    private sealed class AdminCredentialRow
    {
        public int UserID { get; set; }

        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public AdminCredential ToDomain()
        {
            return new AdminCredential(
                LegacyIdMapper.ToGuid(UserID),
                PasswordHash,
                new DateTimeOffset(DateTime.SpecifyKind(CreatedAt, DateTimeKind.Utc)),
                new DateTimeOffset(DateTime.SpecifyKind(UpdatedAt, DateTimeKind.Utc)));
        }
    }
}
