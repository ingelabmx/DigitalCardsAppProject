using DigitalCards.Domain;

namespace DigitalCards.Application.Abstractions;

public interface IBusinessCredentialRepository
{
    Task<BusinessCredential?> FindByBusinessIdAsync(
        Guid businessId,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        BusinessCredential credential,
        CancellationToken cancellationToken = default);
}
