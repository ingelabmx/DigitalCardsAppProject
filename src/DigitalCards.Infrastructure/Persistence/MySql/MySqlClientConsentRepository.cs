using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;
using MySqlConnector;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlClientConsentRepository : IClientConsentRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlClientConsentRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(ClientConsent consent, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into ModernClientConsent (
                UserID,
                BusinessID,
                PolicyVersion,
                Source,
                AcceptedAt)
            values (
                @UserID,
                @BusinessID,
                @PolicyVersion,
                @Source,
                @AcceptedAt);
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    UserID = LegacyIdMapper.ToInt32(consent.ClientId),
                    BusinessID = consent.BusinessId is null ? (int?)null : LegacyIdMapper.ToInt32(consent.BusinessId.Value),
                    consent.PolicyVersion,
                    consent.Source,
                    AcceptedAt = consent.AcceptedAt.UtcDateTime
                },
                cancellationToken: cancellationToken));
        }
        catch (MySqlException exception) when (exception.Number == 1146)
        {
            throw MissingTableException(exception);
        }
    }

    public async Task<IReadOnlyList<ClientConsent>> ListByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select ID,
                   UserID,
                   BusinessID,
                   PolicyVersion,
                   Source,
                   AcceptedAt
            from ModernClientConsent
            where UserID = @UserID
            order by AcceptedAt desc, ID desc;
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            var rows = await connection.QueryAsync<ClientConsentRow>(new CommandDefinition(
                sql,
                new { UserID = LegacyIdMapper.ToInt32(clientId) },
                cancellationToken: cancellationToken));

            return rows.Select(row => row.ToDomain()).ToArray();
        }
        catch (MySqlException exception) when (exception.Number == 1146)
        {
            throw MissingTableException(exception);
        }
    }

    private static InvalidOperationException MissingTableException(MySqlException exception)
    {
        return new InvalidOperationException(
            "ModernClientConsent table is missing. Run docs/migration-context/69-public-enrollment-consent-hostgator.sql before using public consent with MySQL.",
            exception);
    }

    private static DateTimeOffset AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc))
            : new DateTimeOffset(value.ToUniversalTime());
    }

    private sealed class ClientConsentRow
    {
        public long ID { get; set; }

        public int UserID { get; set; }

        public int? BusinessID { get; set; }

        public string PolicyVersion { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;

        public DateTime AcceptedAt { get; set; }

        public ClientConsent ToDomain()
        {
            return new ClientConsent(
                ID,
                LegacyIdMapper.ToGuid(UserID),
                BusinessID is null ? null : LegacyIdMapper.ToGuid(BusinessID.Value),
                PolicyVersion,
                Source,
                AsUtc(AcceptedAt));
        }
    }
}
