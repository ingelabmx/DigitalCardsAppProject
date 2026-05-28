using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;
using MySqlConnector;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlBusinessSubscriptionRepository : IBusinessSubscriptionRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlBusinessSubscriptionRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<BusinessSubscription?> FindByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select BusinessID, StripePlanKey, StripeCustomerId, StripeSubscriptionId,
                   StripeCheckoutSessionId, SubscriptionStatus, MaxClients,
                   SubscriptionEndsAt, GraceEndsAt, CreatedViaSelfService, CreatedAt, UpdatedAt
            from ModernBusinessSubscription
            where BusinessID = @BusinessID
            limit 1;
            """;

        await using var connection = _connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<SubscriptionRow>(
            new CommandDefinition(sql, new { BusinessID = LegacyIdMapper.ToInt32(businessId) }, cancellationToken: cancellationToken));
        return row?.ToDomain();
    }

    public async Task<BusinessSubscription?> FindByCheckoutSessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select BusinessID, StripePlanKey, StripeCustomerId, StripeSubscriptionId,
                   StripeCheckoutSessionId, SubscriptionStatus, MaxClients,
                   SubscriptionEndsAt, GraceEndsAt, CreatedViaSelfService, CreatedAt, UpdatedAt
            from ModernBusinessSubscription
            where StripeCheckoutSessionId = @SessionId
            limit 1;
            """;

        await using var connection = _connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<SubscriptionRow>(
            new CommandDefinition(sql, new { SessionId = sessionId }, cancellationToken: cancellationToken));
        return row?.ToDomain();
    }

    public async Task UpsertAsync(BusinessSubscription subscription, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into ModernBusinessSubscription (
                BusinessID, StripePlanKey, StripeCustomerId, StripeSubscriptionId,
                StripeCheckoutSessionId, SubscriptionStatus, MaxClients,
                SubscriptionEndsAt, GraceEndsAt, CreatedViaSelfService, CreatedAt, UpdatedAt)
            values (
                @BusinessID, @StripePlanKey, @StripeCustomerId, @StripeSubscriptionId,
                @StripeCheckoutSessionId, @SubscriptionStatus, @MaxClients,
                @SubscriptionEndsAt, @GraceEndsAt, @CreatedViaSelfService, @CreatedAt, @UpdatedAt)
            on duplicate key update
                StripePlanKey = values(StripePlanKey),
                StripeCustomerId = values(StripeCustomerId),
                StripeSubscriptionId = values(StripeSubscriptionId),
                StripeCheckoutSessionId = values(StripeCheckoutSessionId),
                SubscriptionStatus = values(SubscriptionStatus),
                MaxClients = values(MaxClients),
                SubscriptionEndsAt = values(SubscriptionEndsAt),
                GraceEndsAt = values(GraceEndsAt),
                UpdatedAt = values(UpdatedAt);
            """;

        await using var connection = _connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            BusinessID = LegacyIdMapper.ToInt32(subscription.BusinessId),
            subscription.StripePlanKey,
            subscription.StripeCustomerId,
            subscription.StripeSubscriptionId,
            subscription.StripeCheckoutSessionId,
            subscription.SubscriptionStatus,
            subscription.MaxClients,
            SubscriptionEndsAt = subscription.SubscriptionEndsAt?.UtcDateTime,
            GraceEndsAt = subscription.GraceEndsAt?.UtcDateTime,
            subscription.CreatedViaSelfService,
            CreatedAt = subscription.CreatedAt.UtcDateTime,
            UpdatedAt = subscription.UpdatedAt.UtcDateTime
        }, cancellationToken: cancellationToken));
    }

    public async Task<BusinessSubscription?> FindByStripeCustomerIdAsync(string customerId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select BusinessID, StripePlanKey, StripeCustomerId, StripeSubscriptionId,
                   StripeCheckoutSessionId, SubscriptionStatus, MaxClients,
                   SubscriptionEndsAt, GraceEndsAt, CreatedViaSelfService, CreatedAt, UpdatedAt
            from ModernBusinessSubscription
            where StripeCustomerId = @CustomerId
            limit 1;
            """;

        await using var connection = _connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<SubscriptionRow>(
            new CommandDefinition(sql, new { CustomerId = customerId }, cancellationToken: cancellationToken));
        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<BusinessSubscription>> ListPastDueGraceExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select BusinessID, StripePlanKey, StripeCustomerId, StripeSubscriptionId,
                   StripeCheckoutSessionId, SubscriptionStatus, MaxClients,
                   SubscriptionEndsAt, GraceEndsAt, CreatedViaSelfService, CreatedAt, UpdatedAt
            from ModernBusinessSubscription
            where SubscriptionStatus = 'past_due'
              and GraceEndsAt is not null
              and GraceEndsAt < @Now;
            """;

        await using var connection = _connectionFactory.Create();
        var rows = await connection.QueryAsync<SubscriptionRow>(
            new CommandDefinition(sql, new { Now = now.UtcDateTime }, cancellationToken: cancellationToken));
        return rows.Select(r => r.ToDomain()).ToArray();
    }

    public async Task<IReadOnlyList<BusinessSubscription>> ListAbandonedAsync(DateTimeOffset createdBefore, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select BusinessID, StripePlanKey, StripeCustomerId, StripeSubscriptionId,
                   StripeCheckoutSessionId, SubscriptionStatus, MaxClients,
                   SubscriptionEndsAt, GraceEndsAt, CreatedViaSelfService, CreatedAt, UpdatedAt
            from ModernBusinessSubscription
            where SubscriptionStatus = 'pending_payment'
              and CreatedAt < @CreatedBefore
            order by CreatedAt desc;
            """;

        await using var connection = _connectionFactory.Create();
        var rows = await connection.QueryAsync<SubscriptionRow>(
            new CommandDefinition(sql, new { CreatedBefore = createdBefore.UtcDateTime }, cancellationToken: cancellationToken));
        return rows.Select(r => r.ToDomain()).ToArray();
    }

    private static DateTimeOffset? AsUtcOffset(DateTime? value)
    {
        if (value is null) return null;
        return value.Value.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
            : new DateTimeOffset(value.Value.ToUniversalTime());
    }

    private static DateTimeOffset AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc))
            : new DateTimeOffset(value.ToUniversalTime());
    }

    private sealed record SubscriptionRow(
        int BusinessID,
        string? StripePlanKey,
        string? StripeCustomerId,
        string? StripeSubscriptionId,
        string? StripeCheckoutSessionId,
        string SubscriptionStatus,
        int MaxClients,
        DateTime? SubscriptionEndsAt,
        DateTime? GraceEndsAt,
        bool CreatedViaSelfService,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        public BusinessSubscription ToDomain() => new(
            LegacyIdMapper.ToGuid(BusinessID),
            SubscriptionStatus,
            MaxClients,
            CreatedViaSelfService,
            AsUtc(CreatedAt),
            AsUtc(UpdatedAt),
            StripePlanKey,
            StripeCustomerId,
            StripeSubscriptionId,
            StripeCheckoutSessionId,
            AsUtcOffset(SubscriptionEndsAt),
            AsUtcOffset(GraceEndsAt));
    }
}
