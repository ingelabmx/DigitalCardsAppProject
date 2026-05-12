using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryStampLedgerRepository : IStampLedgerRepository
{
    private readonly InMemoryDigitalCardsStore _store;

    public InMemoryStampLedgerRepository(InMemoryDigitalCardsStore store)
    {
        _store = store;
    }

    public Task AddAsync(StampLedgerRecord record, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var id = record.Id > 0 ? record.Id : _store.StampLedger.Count + 1;
            _store.StampLedger.Add(record with { Id = id });
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<StampLedgerRecord>> ListRecentByCardIdAsync(
        Guid cardId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var records = _store.StampLedger
                .Where(record => record.CardId == cardId)
                .OrderByDescending(record => record.CreatedAt)
                .ThenByDescending(record => record.Id)
                .Take(Math.Max(1, Math.Min(limit, 50)))
                .ToArray();

            return Task.FromResult<IReadOnlyList<StampLedgerRecord>>(records);
        }
    }
}
