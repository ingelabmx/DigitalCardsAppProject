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
            insert into modern_clients (id, user_name, first_name, last_name, email)
            values (@Id, @UserName, @FirstName, @LastName, @Email);
            """;

        await using var connection = _connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = client.Id.ToString(),
            client.UserName,
            client.FirstName,
            client.LastName,
            client.Email
        }, cancellationToken: cancellationToken));
    }

    public async Task<Client?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id as Id,
                   user_name as User_Name,
                   first_name as First_Name,
                   last_name as Last_Name,
                   email as Email
            from modern_clients
            where id = @Id;
            """;

        await using var connection = _connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<ClientRow>(
            new CommandDefinition(sql, new { Id = id.ToString() }, cancellationToken: cancellationToken));

        return row?.ToDomain();
    }

    public async Task<Client?> FindByUserNameOrEmailAsync(string value, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id as Id,
                   user_name as User_Name,
                   first_name as First_Name,
                   last_name as Last_Name,
                   email as Email
            from modern_clients
            where lower(user_name) = lower(@Value)
               or lower(email) = lower(@Value)
            limit 1;
            """;

        await using var connection = _connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<ClientRow>(
            new CommandDefinition(sql, new { Value = value.Trim() }, cancellationToken: cancellationToken));

        return row?.ToDomain();
    }

    private sealed record ClientRow(
        string Id,
        string User_Name,
        string First_Name,
        string Last_Name,
        string Email)
    {
        public Client ToDomain()
        {
            return new Client(Guid.Parse(Id), User_Name, First_Name, Last_Name, Email);
        }
    }
}
