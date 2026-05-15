using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryRewardRedemptionRepository : IRewardRedemptionRepository
{
    private readonly InMemoryDigitalCardsStore _store;

    public InMemoryRewardRedemptionRepository(InMemoryDigitalCardsStore store)
    {
        _store = store;
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task AddAsync(RewardRedemptionRecord record, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var id = record.Id > 0 ? record.Id : _store.RewardRedemptions.Count + 1;
            _store.RewardRedemptions.Add(record with { Id = id });
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RewardRedemptionRecord>> ListRecentByCardIdAsync(
        Guid cardId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var records = _store.RewardRedemptions
                .Where(record => record.CardId == cardId)
                .OrderByDescending(record => record.RedeemedAt)
                .ThenByDescending(record => record.Id)
                .Take(Math.Max(1, Math.Min(limit, 50)))
                .ToArray();

            return Task.FromResult<IReadOnlyList<RewardRedemptionRecord>>(records);
        }
    }
}
