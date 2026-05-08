using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlBusinessRepository : IBusinessRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlBusinessRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Business?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id as Id,
                   name as Name,
                   email as Email,
                   password_hash_placeholder as Password_Hash_Placeholder,
                   logo_path as Logo_Path
            from modern_businesses
            where lower(email) = lower(@Email)
            limit 1;
            """;

        await using var connection = _connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<BusinessRow>(
            new CommandDefinition(sql, new { Email = email.Trim() }, cancellationToken: cancellationToken));

        return row?.ToDomain();
    }

    public async Task<Business?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id as Id,
                   name as Name,
                   email as Email,
                   password_hash_placeholder as Password_Hash_Placeholder,
                   logo_path as Logo_Path
            from modern_businesses
            where id = @Id;
            """;

        await using var connection = _connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<BusinessRow>(
            new CommandDefinition(sql, new { Id = id.ToString() }, cancellationToken: cancellationToken));

        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<Business>> ListAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id as Id,
                   name as Name,
                   email as Email,
                   password_hash_placeholder as Password_Hash_Placeholder,
                   logo_path as Logo_Path
            from modern_businesses
            order by name;
            """;

        await using var connection = _connectionFactory.Create();
        var rows = await connection.QueryAsync<BusinessRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.Select(row => row.ToDomain()).ToArray();
    }

    private sealed record BusinessRow(
        string Id,
        string Name,
        string Email,
        string Password_Hash_Placeholder,
        string Logo_Path)
    {
        public Business ToDomain()
        {
            return new Business(Guid.Parse(Id), Name, Email, Password_Hash_Placeholder, Logo_Path);
        }
    }
}
