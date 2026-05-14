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

    public Task<LoyaltyCard> AddAsync(LoyaltyCard card, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            _store.LoyaltyCards.Add(card);
        }

        return Task.FromResult(card);
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

    public Task<LoyaltyCard?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.LoyaltyCards.SingleOrDefault(card => card.Id == id));
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

    public Task<IReadOnlyList<LoyaltyCard>> ListByBusinessAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            return Task.FromResult<IReadOnlyList<LoyaltyCard>>(
                _store.LoyaltyCards
                    .Where(card => card.BusinessId == businessId)
                    .OrderByDescending(card => card.LastStampedAt)
                    .ThenByDescending(card => card.CreatedAt)
                    .ToArray());
        }
    }

    public Task<IReadOnlyList<LoyaltyCard>> SearchByBusinessAsync(
        Guid businessId,
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query.Trim();

        lock (_store.Sync)
        {
            var cards = _store.LoyaltyCards
                .Where(card => card.BusinessId == businessId)
                .Where(card => MatchesClient(card.ClientId, normalizedQuery))
                .OrderByDescending(card => card.LastStampedAt)
                .Take(Math.Max(1, limit))
                .ToArray();

            return Task.FromResult<IReadOnlyList<LoyaltyCard>>(cards);
        }
    }

    private bool MatchesClient(Guid clientId, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var client = _store.Clients.SingleOrDefault(candidate => candidate.Id == clientId);
        if (client is null)
        {
            return false;
        }

        return Contains(client.UserName, query) ||
            Contains(client.Email, query) ||
            Contains(client.FirstName, query) ||
            Contains(client.LastName, query) ||
            Contains(client.FullName, query);
    }

    private static bool Contains(string value, string query)
    {
        return value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
