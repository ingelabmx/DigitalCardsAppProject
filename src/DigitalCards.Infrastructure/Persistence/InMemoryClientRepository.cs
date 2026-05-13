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

    public Task<Client> UpdatePasswordAsync(
        Guid clientId,
        string legacyPasswordHash,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var index = _store.Clients.FindIndex(client => client.Id == clientId);
            if (index < 0)
            {
                throw new InvalidOperationException("Client was not found.");
            }

            var existing = _store.Clients[index];
            var updated = new Client(
                existing.Id,
                existing.UserName,
                existing.FirstName,
                existing.LastName,
                existing.Email,
                legacyPasswordHash);
            _store.Clients[index] = updated;
            return Task.FromResult(updated);
        }
    }

    public Task<Client> UpdateProfileAsync(
        Guid clientId,
        string firstName,
        string lastName,
        string email,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var index = _store.Clients.FindIndex(client => client.Id == clientId);
            if (index < 0)
            {
                throw new InvalidOperationException("Client was not found.");
            }

            var existing = _store.Clients[index];
            var updated = new Client(
                existing.Id,
                existing.UserName,
                firstName,
                lastName,
                email,
                existing.PasswordHashPlaceholder);
            _store.Clients[index] = updated;
            return Task.FromResult(updated);
        }
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

    public Task<bool> UserNameOrEmailExistsAsync(string value, CancellationToken cancellationToken = default)
    {
        var normalized = value.Trim();
        lock (_store.Sync)
        {
            return Task.FromResult(
                _store.Clients.Any(client =>
                    string.Equals(client.UserName, normalized, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(client.Email, normalized, StringComparison.OrdinalIgnoreCase)) ||
                _store.AdminUsers.Any(admin =>
                    string.Equals(admin.UserName, normalized, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(admin.Email, normalized, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<bool> EmailExistsForOtherUserAsync(
        Guid clientId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim();
        lock (_store.Sync)
        {
            return Task.FromResult(
                _store.Clients.Any(client =>
                    client.Id != clientId &&
                    string.Equals(client.Email, normalized, StringComparison.OrdinalIgnoreCase)) ||
                _store.AdminUsers.Any(admin =>
                    string.Equals(admin.Email, normalized, StringComparison.OrdinalIgnoreCase)));
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
