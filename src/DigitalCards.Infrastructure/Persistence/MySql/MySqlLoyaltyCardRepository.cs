using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlLoyaltyCardRepository : ILoyaltyCardRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlLoyaltyCardRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(LoyaltyCard card, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into modern_loyalty_cards (
                id,
                client_id,
                business_id,
                enrollment_token,
                current_stamps,
                lifetime_stamps,
                created_at,
                last_stamped_at,
                google_object_id,
                google_save_url)
            values (
                @Id,
                @ClientId,
                @BusinessId,
                @EnrollmentToken,
                @CurrentStamps,
                @LifetimeStamps,
                @CreatedAt,
                @LastStampedAt,
                @GoogleObjectId,
                @GoogleSaveUrl);
            """;

        await using var connection = _connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParameters(card), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(LoyaltyCard card, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update modern_loyalty_cards
            set current_stamps = @CurrentStamps,
                lifetime_stamps = @LifetimeStamps,
                last_stamped_at = @LastStampedAt,
                google_object_id = @GoogleObjectId,
                google_save_url = @GoogleSaveUrl
            where id = @Id;
            """;

        await using var connection = _connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParameters(card), cancellationToken: cancellationToken));
    }

    public async Task<LoyaltyCard?> FindByClientAndBusinessAsync(
        Guid clientId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id as Id,
                   client_id as Client_Id,
                   business_id as Business_Id,
                   enrollment_token as Enrollment_Token,
                   current_stamps as Current_Stamps,
                   lifetime_stamps as Lifetime_Stamps,
                   created_at as Created_At,
                   last_stamped_at as Last_Stamped_At,
                   google_object_id as Google_Object_Id,
                   google_save_url as Google_Save_Url
            from modern_loyalty_cards
            where client_id = @ClientId
              and business_id = @BusinessId
            limit 1;
            """;

        await using var connection = _connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<LoyaltyCardRow>(
            new CommandDefinition(sql, new
            {
                ClientId = clientId.ToString(),
                BusinessId = businessId.ToString()
            }, cancellationToken: cancellationToken));

        return row?.ToDomain();
    }

    public async Task<LoyaltyCard?> FindByEnrollmentTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id as Id,
                   client_id as Client_Id,
                   business_id as Business_Id,
                   enrollment_token as Enrollment_Token,
                   current_stamps as Current_Stamps,
                   lifetime_stamps as Lifetime_Stamps,
                   created_at as Created_At,
                   last_stamped_at as Last_Stamped_At,
                   google_object_id as Google_Object_Id,
                   google_save_url as Google_Save_Url
            from modern_loyalty_cards
            where enrollment_token = @Token
            limit 1;
            """;

        await using var connection = _connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<LoyaltyCardRow>(
            new CommandDefinition(sql, new { Token = token.Trim() }, cancellationToken: cancellationToken));

        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<LoyaltyCard>> ListByClientAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id as Id,
                   client_id as Client_Id,
                   business_id as Business_Id,
                   enrollment_token as Enrollment_Token,
                   current_stamps as Current_Stamps,
                   lifetime_stamps as Lifetime_Stamps,
                   created_at as Created_At,
                   last_stamped_at as Last_Stamped_At,
                   google_object_id as Google_Object_Id,
                   google_save_url as Google_Save_Url
            from modern_loyalty_cards
            where client_id = @ClientId
            order by created_at desc;
            """;

        await using var connection = _connectionFactory.Create();
        var rows = await connection.QueryAsync<LoyaltyCardRow>(
            new CommandDefinition(sql, new { ClientId = clientId.ToString() }, cancellationToken: cancellationToken));

        return rows.Select(row => row.ToDomain()).ToArray();
    }

    private static object ToParameters(LoyaltyCard card)
    {
        return new
        {
            Id = card.Id.ToString(),
            ClientId = card.ClientId.ToString(),
            BusinessId = card.BusinessId.ToString(),
            card.EnrollmentToken,
            card.CurrentStamps,
            card.LifetimeStamps,
            CreatedAt = card.CreatedAt.UtcDateTime,
            LastStampedAt = card.LastStampedAt.UtcDateTime,
            card.GoogleObjectId,
            card.GoogleSaveUrl
        };
    }

    private static DateTimeOffset AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc))
            : new DateTimeOffset(value.ToUniversalTime());
    }

    private sealed record LoyaltyCardRow(
        string Id,
        string Client_Id,
        string Business_Id,
        string Enrollment_Token,
        int Current_Stamps,
        int Lifetime_Stamps,
        DateTime Created_At,
        DateTime Last_Stamped_At,
        string? Google_Object_Id,
        string? Google_Save_Url)
    {
        public LoyaltyCard ToDomain()
        {
            return LoyaltyCard.Restore(
                Guid.Parse(Id),
                Guid.Parse(Client_Id),
                Guid.Parse(Business_Id),
                Enrollment_Token,
                Current_Stamps,
                Lifetime_Stamps,
                AsUtc(Created_At),
                AsUtc(Last_Stamped_At),
                Google_Object_Id,
                Google_Save_Url);
        }
    }
}
