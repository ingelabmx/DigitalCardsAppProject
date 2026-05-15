using DigitalCards.Application.Models;

namespace DigitalCards.Application.Abstractions;

public interface IRewardRedemptionRepository
{
    Task AddAsync(RewardRedemptionRecord record, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RewardRedemptionRecord>> ListRecentByCardIdAsync(
        Guid cardId,
        int limit,
        CancellationToken cancellationToken = default);
}
