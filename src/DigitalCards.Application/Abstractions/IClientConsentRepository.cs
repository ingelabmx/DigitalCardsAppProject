using DigitalCards.Domain;

namespace DigitalCards.Application.Abstractions;

public interface IClientConsentRepository
{
    Task AddAsync(ClientConsent consent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClientConsent>> ListByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default);
}
