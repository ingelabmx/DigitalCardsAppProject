using DigitalCards.Domain;

namespace DigitalCards.Application.Abstractions;

public interface IBusinessSubscriptionRepository
{
    Task<BusinessSubscription?> FindByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default);

    Task<BusinessSubscription?> FindByCheckoutSessionIdAsync(string sessionId, CancellationToken cancellationToken = default);

    Task<BusinessSubscription?> FindByStripeCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);

    Task UpsertAsync(BusinessSubscription subscription, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BusinessSubscription>> ListPastDueGraceExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BusinessSubscription>> ListAbandonedAsync(DateTimeOffset createdBefore, CancellationToken cancellationToken = default);
}
