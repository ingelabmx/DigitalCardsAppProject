using DigitalCards.Application.Models;

namespace DigitalCards.Application.Abstractions;

public interface ILegacyWalletSyncRepository
{
    Task<IReadOnlyList<LegacyWalletSyncCandidate>> ListCandidatesAsync(
        DateTimeOffset changedSince,
        int batchSize,
        CancellationToken cancellationToken = default);
}
