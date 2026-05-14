using DigitalCards.Application.Models;

namespace DigitalCards.Application.Abstractions;

public interface IAccountLifecycleRepository
{
    Task<ClientCardLifecycleRecord?> FindCardLifecycleAsync(
        Guid cardId,
        CancellationToken cancellationToken = default);

    Task SetCardActiveAsync(
        ClientCardLifecycleRecord status,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteBusinessCardAsync(
        Guid businessId,
        Guid cardId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteBusinessAsync(
        Guid businessId,
        CancellationToken cancellationToken = default);
}
