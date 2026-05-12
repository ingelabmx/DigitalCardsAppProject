using DigitalCards.Domain;

namespace DigitalCards.Application.Abstractions;

public interface IPilotClientRepository
{
    Task<PilotClientAccess?> FindByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PilotClientAccess>> ListAsync(CancellationToken cancellationToken = default);

    Task UpsertAsync(PilotClientAccess access, CancellationToken cancellationToken = default);
}
