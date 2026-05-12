using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryAdminUserRepository : IAdminUserRepository
{
    private const string DuplicateAdminMessage = "An admin user with the same username or email already exists.";

    private readonly InMemoryDigitalCardsStore _store;

    public InMemoryAdminUserRepository(InMemoryDigitalCardsStore store)
    {
        _store = store;
    }

    public Task<AdminUser> AddAsync(
        AdminUser admin,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            if (UserNameOrEmailExists(admin.UserName) || UserNameOrEmailExists(admin.Email))
            {
                throw new InvalidOperationException(DuplicateAdminMessage);
            }

            _store.AdminUsers.Add(admin);
            return Task.FromResult(admin);
        }
    }

    public Task<AdminUser?> FindByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            return Task.FromResult(_store.AdminUsers.SingleOrDefault(admin => admin.Id == id));
        }
    }

    public Task<AdminUser?> FindByUserNameOrEmailAsync(
        string value,
        CancellationToken cancellationToken = default)
    {
        var normalized = value.Trim();
        lock (_store.Sync)
        {
            return Task.FromResult(_store.AdminUsers.SingleOrDefault(admin =>
                string.Equals(admin.UserName, normalized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(admin.Email, normalized, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<IReadOnlyList<AdminUser>> ListAsync(CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            return Task.FromResult<IReadOnlyList<AdminUser>>(
                _store.AdminUsers
                    .OrderBy(admin => admin.UserName, StringComparer.OrdinalIgnoreCase)
                    .ToArray());
        }
    }

    public Task<AdminUser> UpdatePasswordAsync(
        Guid id,
        string legacyPasswordHash,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var index = _store.AdminUsers.FindIndex(admin => admin.Id == id);
            if (index < 0)
            {
                throw new InvalidOperationException("Admin user was not found.");
            }

            var current = _store.AdminUsers[index];
            var updated = new AdminUser(
                current.Id,
                current.UserName,
                current.FirstName,
                current.LastName,
                current.Email,
                legacyPasswordHash);
            _store.AdminUsers[index] = updated;
            return Task.FromResult(updated);
        }
    }

    private bool UserNameOrEmailExists(string value)
    {
        return _store.AdminUsers.Any(admin =>
                string.Equals(admin.UserName, value, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(admin.Email, value, StringComparison.OrdinalIgnoreCase)) ||
            _store.Clients.Any(client =>
                string.Equals(client.UserName, value, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(client.Email, value, StringComparison.OrdinalIgnoreCase));
    }
}
