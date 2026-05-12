using DigitalCards.Domain;

namespace DigitalCards.Application.Abstractions;

public interface IPilotBusinessRepository
{
    Task<PilotBusinessAccess?> FindByBusinessIdAsync(
        Guid businessId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PilotBusinessAccess>> ListAsync(CancellationToken cancellationToken = default);

    Task UpsertAsync(PilotBusinessAccess access, CancellationToken cancellationToken = default);
}
