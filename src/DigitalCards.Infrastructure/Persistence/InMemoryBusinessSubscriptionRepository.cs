using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryBusinessSubscriptionRepository : IBusinessSubscriptionRepository
{
    private readonly List<BusinessSubscription> _subscriptions = [];
    private readonly object _sync = new();

    public Task<BusinessSubscription?> FindByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_subscriptions.SingleOrDefault(s => s.BusinessId == businessId));
        }
    }

    public Task<BusinessSubscription?> FindByCheckoutSessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_subscriptions.SingleOrDefault(s => s.StripeCheckoutSessionId == sessionId));
        }
    }

    public Task<BusinessSubscription?> FindByStripeCustomerIdAsync(string customerId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_subscriptions.SingleOrDefault(s => s.StripeCustomerId == customerId));
        }
    }

    public Task UpsertAsync(BusinessSubscription subscription, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var index = _subscriptions.FindIndex(s => s.BusinessId == subscription.BusinessId);
            if (index >= 0)
                _subscriptions[index] = subscription;
            else
                _subscriptions.Add(subscription);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BusinessSubscription>> ListPastDueGraceExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var result = _subscriptions
                .Where(s => s.SubscriptionStatus == "past_due" && s.GraceEndsAt.HasValue && s.GraceEndsAt.Value < now)
                .ToArray();
            return Task.FromResult<IReadOnlyList<BusinessSubscription>>(result);
        }
    }

    public Task<IReadOnlyList<BusinessSubscription>> ListAbandonedAsync(DateTimeOffset createdBefore, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var result = _subscriptions
                .Where(s => s.SubscriptionStatus == "pending_payment" && s.CreatedAt < createdBefore)
                .OrderByDescending(s => s.CreatedAt)
                .ToArray();
            return Task.FromResult<IReadOnlyList<BusinessSubscription>>(result);
        }
    }
}
