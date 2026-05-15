using DigitalCards.Application.Models;

namespace DigitalCards.Application.Abstractions;

public interface IRewardRedemptionRepository
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    Task AddAsync(RewardRedemptionRecord record, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RewardRedemptionRecord>> ListRecentByCardIdAsync(
        Guid cardId,
        int limit,
        CancellationToken cancellationToken = default);
}
