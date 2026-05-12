using DigitalCards.Application.Models;

namespace DigitalCards.Application.Abstractions;

public interface IStampLedgerRepository
{
    Task AddAsync(StampLedgerRecord record, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StampLedgerRecord>> ListRecentByCardIdAsync(
        Guid cardId,
        int limit,
        CancellationToken cancellationToken = default);
}
