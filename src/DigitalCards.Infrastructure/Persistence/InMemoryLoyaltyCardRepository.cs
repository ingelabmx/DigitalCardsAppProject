using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryLoyaltyCardRepository : ILoyaltyCardRepository
{
    private readonly InMemoryDigitalCardsStore _store;

    public InMemoryLoyaltyCardRepository(InMemoryDigitalCardsStore store)
    {
        _store = store;
    }

    public Task AddAsync(LoyaltyCard card, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            _store.LoyaltyCards.Add(card);
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(LoyaltyCard card, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var index = _store.LoyaltyCards.FindIndex(existing => existing.Id == card.Id);
            if (index >= 0)
            {
                _store.LoyaltyCards[index] = card;
            }
        }

        return Task.CompletedTask;
    }

    public Task<LoyaltyCard?> FindByClientAndBusinessAsync(
        Guid clientId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.LoyaltyCards.SingleOrDefault(card =>
            card.ClientId == clientId && card.BusinessId == businessId));
    }

    public Task<LoyaltyCard?> FindByEnrollmentTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.LoyaltyCards.SingleOrDefault(card =>
            string.Equals(card.EnrollmentToken, token, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<IReadOnlyList<LoyaltyCard>> ListByClientAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<LoyaltyCard>>(
            _store.LoyaltyCards.Where(card => card.ClientId == clientId).ToArray());
    }
}
