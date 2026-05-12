using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryPilotClientRepository : IPilotClientRepository
{
    private readonly InMemoryDigitalCardsStore _store;

    public InMemoryPilotClientRepository(InMemoryDigitalCardsStore store)
    {
        _store = store;
    }

    public Task<PilotClientAccess?> FindByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            return Task.FromResult(_store.PilotClients.SingleOrDefault(access =>
                access.ClientId == clientId));
        }
    }

    public Task<IReadOnlyList<PilotClientAccess>> ListAsync(CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            return Task.FromResult<IReadOnlyList<PilotClientAccess>>(_store.PilotClients.ToArray());
        }
    }

    public Task UpsertAsync(PilotClientAccess access, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var index = _store.PilotClients.FindIndex(existing => existing.ClientId == access.ClientId);
            if (index >= 0)
            {
                _store.PilotClients[index] = access;
            }
            else
            {
                _store.PilotClients.Add(access);
            }
        }

        return Task.CompletedTask;
    }
}
