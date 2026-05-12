using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;
using MySqlConnector;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlAdminUserRepository : IAdminUserRepository
{
    private const string DuplicateAdminMessage = "An admin user with the same username or email already exists.";

    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlAdminUserRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AdminUser> AddAsync(
        AdminUser admin,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into UserClient (UserName, UserPassword, FirstName, Lastname, UserEmail, RoleID)
            values (@UserName, @UserPassword, @FirstName, @LastName, @UserEmail, 1);
            select last_insert_id();
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            var userId = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        admin.UserName,
                        UserPassword = admin.PasswordHashPlaceholder,
                        admin.FirstName,
                        LastName = admin.LastName,
                        UserEmail = admin.Email
                    },
                    cancellationToken: cancellationToken));

            return await FindByLegacyIdAsync(userId, cancellationToken)
                ?? throw new InvalidOperationException("Inserted legacy admin could not be loaded.");
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            throw new InvalidOperationException(DuplicateAdminMessage, ex);
        }
    }

    public async Task<AdminUser?> FindByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await FindByLegacyIdAsync(LegacyIdMapper.ToInt32(id), cancellationToken);
    }

    public async Task<AdminUser?> FindByUserNameOrEmailAsync(
        string value,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select UserID,
                   UserName,
                   UserPassword,
                   FirstName,
                   Lastname,
                   UserEmail
            from UserClient
            where RoleID = 1
              and (
                    lower(UserName) = lower(@Value)
                 or lower(UserEmail) = lower(@Value)
              )
            limit 1;
            """;

        await using var connection = _connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<AdminUserRow>(
            new CommandDefinition(sql, new { Value = value.Trim() }, cancellationToken: cancellationToken));

        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<AdminUser>> ListAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            select UserID,
                   UserName,
                   UserPassword,
                   FirstName,
                   Lastname,
                   UserEmail
            from UserClient
            where RoleID = 1
            order by UserName;
            """;

        await using var connection = _connectionFactory.Create();
        var rows = await connection.QueryAsync<AdminUserRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.Select(row => row.ToDomain()).ToArray();
    }

    public async Task<AdminUser> UpdatePasswordAsync(
        Guid id,
        string legacyPasswordHash,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            update UserClient
            set UserPassword = @UserPassword
            where UserID = @UserId
              and RoleID = 1;
            """;

        await using var connection = _connectionFactory.Create();
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                UserId = LegacyIdMapper.ToInt32(id),
                UserPassword = legacyPasswordHash
            },
            cancellationToken: cancellationToken));

        if (affected == 0)
        {
            throw new InvalidOperationException("Admin user was not found.");
        }

        return await FindByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Updated legacy admin could not be loaded.");
    }

    private async Task<AdminUser?> FindByLegacyIdAsync(int userId, CancellationToken cancellationToken)
    {
        const string sql = """
            select UserID,
                   UserName,
                   UserPassword,
                   FirstName,
                   Lastname,
                   UserEmail
            from UserClient
            where UserID = @UserId
              and RoleID = 1
            limit 1;
            """;

        await using var connection = _connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<AdminUserRow>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));

        return row?.ToDomain();
    }

    private sealed class AdminUserRow
    {
        public int UserID { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string UserPassword { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string Lastname { get; set; } = string.Empty;

        public string UserEmail { get; set; } = string.Empty;

        public AdminUser ToDomain()
        {
            return new AdminUser(
                LegacyIdMapper.ToGuid(UserID),
                UserName ?? string.Empty,
                FirstName ?? string.Empty,
                Lastname ?? string.Empty,
                UserEmail ?? string.Empty,
                UserPassword ?? string.Empty);
        }
    }
}
