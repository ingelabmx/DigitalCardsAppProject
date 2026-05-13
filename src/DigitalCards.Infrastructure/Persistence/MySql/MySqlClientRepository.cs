using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlClientRepository : IClientRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlClientRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into UserClient (UserName, UserPassword, FirstName, Lastname, UserEmail, RoleID)
            values (@UserName, @UserPassword, @FirstName, @LastName, @Email, 2);
            """;

        await using var connection = _connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            client.UserName,
            UserPassword = client.PasswordHashPlaceholder,
            client.FirstName,
            client.LastName,
            client.Email
        }, cancellationToken: cancellationToken));
    }

    public async Task<Client?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select UserID,
                   UserName,
                   UserPassword,
                   FirstName,
                   Lastname,
                   UserEmail
            from UserClient
            where UserID = @Id
              and RoleID = 2;
            """;

        await using var connection = _connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<ClientRow>(
            new CommandDefinition(sql, new { Id = LegacyIdMapper.ToInt32(id) }, cancellationToken: cancellationToken));

        return row?.ToDomain();
    }

    public async Task<Client?> FindByUserNameOrEmailAsync(string value, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select UserID,
                   UserName,
                   UserPassword,
                   FirstName,
                   Lastname,
                   UserEmail
            from UserClient
            where RoleID = 2
              and (
                    lower(UserName) = lower(@Value)
                 or lower(UserEmail) = lower(@Value)
              )
            limit 1;
            """;

        await using var connection = _connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<ClientRow>(
            new CommandDefinition(sql, new { Value = value.Trim() }, cancellationToken: cancellationToken));

        return row?.ToDomain();
    }

    public async Task<bool> UserNameOrEmailExistsAsync(
        string value,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select 1
            from UserClient
            where lower(UserName) = lower(@Value)
               or lower(UserEmail) = lower(@Value)
            limit 1;
            """;

        await using var connection = _connectionFactory.Create();
        var exists = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(sql, new { Value = value.Trim() }, cancellationToken: cancellationToken));

        return exists is not null;
    }

    public async Task<IReadOnlyList<Client>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select UserID,
                   UserName,
                   FirstName,
                   Lastname,
                   UserEmail
            from UserClient
            where RoleID = 2
              and (
                    @Query = ''
                 or lower(UserName) like concat('%', lower(@Query), '%')
                 or lower(UserEmail) like concat('%', lower(@Query), '%')
                 or lower(FirstName) like concat('%', lower(@Query), '%')
                 or lower(Lastname) like concat('%', lower(@Query), '%')
              )
            order by UserName
            limit 50;
            """;

        await using var connection = _connectionFactory.Create();
        var rows = await connection.QueryAsync<ClientRow>(
            new CommandDefinition(sql, new { Query = query.Trim() }, cancellationToken: cancellationToken));

        return rows.Select(row => row.ToDomain()).ToArray();
    }

    private sealed class ClientRow
    {
        public int UserID { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string UserPassword { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string Lastname { get; set; } = string.Empty;

        public string UserEmail { get; set; } = string.Empty;

        public Client ToDomain()
        {
            return new Client(
                LegacyIdMapper.ToGuid(UserID),
                UserName,
                FirstName,
                Lastname,
                UserEmail,
                UserPassword);
        }
    }
}
