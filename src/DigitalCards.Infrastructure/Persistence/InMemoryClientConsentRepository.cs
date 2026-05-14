using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryClientConsentRepository : IClientConsentRepository
{
    private readonly InMemoryDigitalCardsStore _store;

    public InMemoryClientConsentRepository(InMemoryDigitalCardsStore store)
    {
        _store = store;
    }

    public Task AddAsync(ClientConsent consent, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var id = consent.Id > 0 ? consent.Id : _store.ClientConsents.Count + 1;
            _store.ClientConsents.Add(consent with { Id = id });
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ClientConsent>> ListByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            return Task.FromResult<IReadOnlyList<ClientConsent>>(
                _store.ClientConsents
                    .Where(consent => consent.ClientId == clientId)
                    .OrderByDescending(consent => consent.AcceptedAt)
                    .ThenByDescending(consent => consent.Id)
                    .ToArray());
        }
    }
}
