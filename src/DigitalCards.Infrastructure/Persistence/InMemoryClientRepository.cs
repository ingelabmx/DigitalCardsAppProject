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
        lock (_store.Sync)
        {
            return Task.FromResult(_store.Clients.SingleOrDefault(client => client.Id == id));
        }
    }

    public Task<Client?> FindByUserNameOrEmailAsync(string value, CancellationToken cancellationToken = default)
    {
        var normalized = value.Trim();
        lock (_store.Sync)
        {
            return Task.FromResult(_store.Clients.SingleOrDefault(client =>
                string.Equals(client.UserName, normalized, StringComparison.OrdinalIgnoreCase)
                || string.Equals(client.Email, normalized, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<IReadOnlyList<Client>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var normalized = query.Trim();
        lock (_store.Sync)
        {
            return Task.FromResult<IReadOnlyList<Client>>(
                _store.Clients
                    .Where(client => Matches(client, normalized))
                    .OrderBy(client => client.UserName, StringComparer.OrdinalIgnoreCase)
                    .Take(50)
                    .ToArray());
        }
    }

    private static bool Matches(Client client, string query)
    {
        return string.IsNullOrWhiteSpace(query) ||
            client.UserName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            client.Email.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            client.FirstName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            client.LastName.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
