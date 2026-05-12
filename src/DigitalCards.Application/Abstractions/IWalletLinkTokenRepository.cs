using DigitalCards.Application.Models;

namespace DigitalCards.Application.Abstractions;

public interface IWalletLinkTokenRepository
{
    Task AddAsync(WalletLinkTokenRecord token, CancellationToken cancellationToken = default);

    Task<WalletLinkTokenRecord?> FindActiveByTokenHashAsync(
        string tokenHash,
        string purpose,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WalletLinkTokenRecord>> ListActiveByCardIdAsync(
        Guid cardId,
        string purpose,
        CancellationToken cancellationToken = default);

    Task MarkUsedAsync(
        string tokenHash,
        string purpose,
        DateTimeOffset usedAt,
        CancellationToken cancellationToken = default);

    Task RevokeActiveByCardIdAsync(
        Guid cardId,
        string purpose,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken = default);
}
