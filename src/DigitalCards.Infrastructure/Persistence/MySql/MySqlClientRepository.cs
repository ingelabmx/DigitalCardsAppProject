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
            values (@UserName, '', @FirstName, @LastName, @Email, 2);
            """;

        await using var connection = _connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            client.UserName,
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
                   FirstName,
                   Lastname,
                   UserEmail
            from UserClient
            where UserID = @Id;
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
                   FirstName,
                   Lastname,
                   UserEmail
            from UserClient
            where lower(UserName) = lower(@Value)
               or lower(UserEmail) = lower(@Value)
            limit 1;
            """;

        await using var connection = _connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<ClientRow>(
            new CommandDefinition(sql, new { Value = value.Trim() }, cancellationToken: cancellationToken));

        return row?.ToDomain();
    }

    private sealed record ClientRow(
        int UserID,
        string UserName,
        string FirstName,
        string Lastname,
        string UserEmail)
    {
        public Client ToDomain()
        {
            return new Client(LegacyIdMapper.ToGuid(UserID), UserName, FirstName, Lastname, UserEmail);
        }
    }
}
