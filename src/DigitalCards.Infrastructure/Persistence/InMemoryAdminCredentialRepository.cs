using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryAdminCredentialRepository : IAdminCredentialRepository
{
    private readonly InMemoryDigitalCardsStore _store;

    public InMemoryAdminCredentialRepository(InMemoryDigitalCardsStore store)
    {
        _store = store;
    }

    public Task<AdminCredential?> FindByAdminUserIdAsync(
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            return Task.FromResult(_store.AdminCredentials.SingleOrDefault(
                credential => credential.AdminUserId == adminUserId));
        }
    }

    public Task UpsertAsync(
        AdminCredential credential,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var index = _store.AdminCredentials.FindIndex(
                existing => existing.AdminUserId == credential.AdminUserId);
            if (index >= 0)
            {
                _store.AdminCredentials[index] = credential;
            }
            else
            {
                _store.AdminCredentials.Add(credential);
            }
        }

        return Task.CompletedTask;
    }
}
