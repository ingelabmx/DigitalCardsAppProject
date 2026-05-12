using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;
using MySqlConnector;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlBusinessRepository : IBusinessRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlBusinessRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Business> AddAsync(Business business, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into Business (BusinessName, BusinessPassword, BusinessEmail, BusinessLogo)
            values (@BusinessName, @BusinessPassword, @BusinessEmail, @BusinessLogo);
            select last_insert_id();
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            var businessId = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        BusinessName = business.Name,
                        BusinessPassword = business.PasswordHashPlaceholder,
                        BusinessEmail = business.Email,
                        BusinessLogo = business.LogoPath
                    },
                    cancellationToken: cancellationToken));

            return await FindByLegacyIdAsync(businessId, cancellationToken)
                ?? throw new InvalidOperationException("Inserted legacy business could not be loaded.");
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            throw new InvalidOperationException(
                "A business with the same name or email already exists.",
                ex);
        }
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
        return await FindByLegacyIdAsync(LegacyIdMapper.ToInt32(id), cancellationToken);
    }

    public async Task<Business?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select BusinessID,
                   BusinessName,
                   BusinessEmail,
                   BusinessPassword,
                   BusinessLogo
            from Business
            where lower(BusinessName) = lower(@Name)
            limit 1;
            """;

        await using var connection = _connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<BusinessRow>(
            new CommandDefinition(sql, new { Name = name.Trim() }, cancellationToken: cancellationToken));

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

    private async Task<Business?> FindByLegacyIdAsync(int businessId, CancellationToken cancellationToken)
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
            new CommandDefinition(sql, new { Id = businessId }, cancellationToken: cancellationToken));

        return row?.ToDomain();
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
