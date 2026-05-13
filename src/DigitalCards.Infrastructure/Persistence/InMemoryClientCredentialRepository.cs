using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryClientCredentialRepository : IClientCredentialRepository
{
    private readonly InMemoryDigitalCardsStore _store;

    public InMemoryClientCredentialRepository(InMemoryDigitalCardsStore store)
    {
        _store = store;
    }

    public Task<ClientCredential?> FindByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            return Task.FromResult(_store.ClientCredentials.SingleOrDefault(
                credential => credential.ClientId == clientId));
        }
    }

    public Task UpsertAsync(
        ClientCredential credential,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var index = _store.ClientCredentials.FindIndex(
                existing => existing.ClientId == credential.ClientId);
            if (index >= 0)
            {
                _store.ClientCredentials[index] = credential;
            }
            else
            {
                _store.ClientCredentials.Add(credential);
            }
        }

        return Task.CompletedTask;
    }
}
