using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryPilotBusinessRepository : IPilotBusinessRepository
{
    private readonly InMemoryDigitalCardsStore _store;

    public InMemoryPilotBusinessRepository(InMemoryDigitalCardsStore store)
    {
        _store = store;
    }

    public Task<PilotBusinessAccess?> FindByBusinessIdAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            return Task.FromResult(_store.PilotBusinesses.SingleOrDefault(access =>
                access.BusinessId == businessId));
        }
    }

    public Task<IReadOnlyList<PilotBusinessAccess>> ListAsync(CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            return Task.FromResult<IReadOnlyList<PilotBusinessAccess>>(_store.PilotBusinesses.ToArray());
        }
    }

    public Task UpsertAsync(PilotBusinessAccess access, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var index = _store.PilotBusinesses.FindIndex(existing => existing.BusinessId == access.BusinessId);
            if (index >= 0)
            {
                _store.PilotBusinesses[index] = access;
            }
            else
            {
                _store.PilotBusinesses.Add(access);
            }
        }

        return Task.CompletedTask;
    }
}
