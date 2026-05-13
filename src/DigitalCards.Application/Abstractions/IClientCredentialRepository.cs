using DigitalCards.Domain;

namespace DigitalCards.Application.Abstractions;

public interface IClientCredentialRepository
{
    Task<ClientCredential?> FindByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        ClientCredential credential,
        CancellationToken cancellationToken = default);
}
