using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryAdminUserRepository : IAdminUserRepository
{
    private readonly InMemoryDigitalCardsStore _store;

    public InMemoryAdminUserRepository(InMemoryDigitalCardsStore store)
    {
        _store = store;
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
}
