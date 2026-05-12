using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlAdminUserRepository : IAdminUserRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlAdminUserRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
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

    private sealed record AdminUserRow(
        int UserID,
        string UserName,
        string UserPassword,
        string FirstName,
        string Lastname,
        string UserEmail)
    {
        public AdminUser ToDomain()
        {
            return new AdminUser(
                LegacyIdMapper.ToGuid(UserID),
                UserName,
                FirstName,
                Lastname,
                UserEmail,
                UserPassword);
        }
    }
}
