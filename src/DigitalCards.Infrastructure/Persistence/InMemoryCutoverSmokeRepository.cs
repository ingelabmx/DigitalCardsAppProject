using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryCutoverSmokeRepository : ICutoverSmokeRepository
{
    private readonly InMemoryDigitalCardsStore _store;

    public InMemoryCutoverSmokeRepository(InMemoryDigitalCardsStore store)
    {
        _store = store;
    }

    public Task AddAsync(CutoverSmokeEvidence evidence, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var id = evidence.Id > 0 ? evidence.Id : _store.CutoverSmokeEvidence.Count + 1;
            _store.CutoverSmokeEvidence.Add(evidence with { Id = id });
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CutoverSmokeEvidence>> ListRecentByBusinessIdAsync(
        Guid businessId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var records = _store.CutoverSmokeEvidence
                .Where(evidence => evidence.BusinessId == businessId)
                .OrderByDescending(evidence => evidence.CreatedAt)
                .ThenByDescending(evidence => evidence.Id)
                .Take(Math.Max(1, Math.Min(limit, 25)))
                .ToArray();

            return Task.FromResult<IReadOnlyList<CutoverSmokeEvidence>>(records);
        }
    }
}
