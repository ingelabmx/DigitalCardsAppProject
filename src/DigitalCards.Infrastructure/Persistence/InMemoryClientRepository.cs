using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryClientRepository : IClientRepository
{
    private readonly InMemoryDigitalCardsStore _store;

    public InMemoryClientRepository(InMemoryDigitalCardsStore store)
    {
        _store = store;
    }

    public Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            _store.Clients.Add(client);
        }

        return Task.CompletedTask;
    }

    public Task<Client?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.Clients.SingleOrDefault(client => client.Id == id));
    }

    public Task<Client?> FindByUserNameOrEmailAsync(string value, CancellationToken cancellationToken = default)
    {
        var normalized = value.Trim();
        return Task.FromResult(_store.Clients.SingleOrDefault(client =>
            string.Equals(client.UserName, normalized, StringComparison.OrdinalIgnoreCase)
            || string.Equals(client.Email, normalized, StringComparison.OrdinalIgnoreCase)));
    }
}

