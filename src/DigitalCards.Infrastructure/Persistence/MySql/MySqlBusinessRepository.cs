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
            select BusinessID,
                   BusinessName,
                   BusinessEmail,
                   BusinessPassword,
                   BusinessLogo
            from Business
            where lower(BusinessEmail) = lower(@Email)
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
            select BusinessID,
                   BusinessName,
                   BusinessEmail,
                   BusinessPassword,
                   BusinessLogo
            from Business
            where BusinessID = @Id;
            """;

        await using var connection = _connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<BusinessRow>(
            new CommandDefinition(sql, new { Id = LegacyIdMapper.ToInt32(id) }, cancellationToken: cancellationToken));

        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<Business>> ListAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            select BusinessID,
                   BusinessName,
                   BusinessEmail,
                   BusinessPassword,
                   BusinessLogo
            from Business
            order by BusinessName;
            """;

        await using var connection = _connectionFactory.Create();
        var rows = await connection.QueryAsync<BusinessRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.Select(row => row.ToDomain()).ToArray();
    }

    private sealed record BusinessRow(
        int BusinessID,
        string BusinessName,
        string BusinessEmail,
        string BusinessPassword,
        string? BusinessLogo)
    {
        public Business ToDomain()
        {
            return new Business(
                LegacyIdMapper.ToGuid(BusinessID),
                BusinessName,
                BusinessEmail,
                BusinessPassword,
                string.IsNullOrWhiteSpace(BusinessLogo) ? "/img/demo-coffee.svg" : BusinessLogo);
        }
    }
}
