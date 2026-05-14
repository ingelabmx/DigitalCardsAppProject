using DigitalCards.Domain;

namespace DigitalCards.Application.Abstractions;

public interface ICutoverSmokeRepository
{
    Task AddAsync(CutoverSmokeEvidence evidence, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CutoverSmokeEvidence>> ListRecentByBusinessIdAsync(
        Guid businessId,
        int limit,
        CancellationToken cancellationToken = default);
}
