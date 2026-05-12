using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryBusinessCredentialRepository : IBusinessCredentialRepository
{
    private readonly InMemoryDigitalCardsStore _store;

    public InMemoryBusinessCredentialRepository(InMemoryDigitalCardsStore store)
    {
        _store = store;
    }

    public Task<BusinessCredential?> FindByBusinessIdAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            return Task.FromResult(_store.BusinessCredentials.SingleOrDefault(
                credential => credential.BusinessId == businessId));
        }
    }

    public Task UpsertAsync(
        BusinessCredential credential,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var index = _store.BusinessCredentials.FindIndex(
                existing => existing.BusinessId == credential.BusinessId);
            if (index >= 0)
            {
                _store.BusinessCredentials[index] = credential;
            }
            else
            {
                _store.BusinessCredentials.Add(credential);
            }
        }

        return Task.CompletedTask;
    }
}
