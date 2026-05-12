using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryWalletLinkTokenRepository : IWalletLinkTokenRepository
{
    private readonly InMemoryDigitalCardsStore _store;

    public InMemoryWalletLinkTokenRepository(InMemoryDigitalCardsStore store)
    {
        _store = store;
    }

    public Task AddAsync(WalletLinkTokenRecord token, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            _store.WalletLinkTokens.Add(token);
        }

        return Task.CompletedTask;
    }

    public Task<WalletLinkTokenRecord?> FindActiveByTokenHashAsync(
        string tokenHash,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            return Task.FromResult(_store.WalletLinkTokens.SingleOrDefault(token =>
                token.IsActive &&
                string.Equals(token.TokenHash, tokenHash, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(token.Purpose, purpose, StringComparison.Ordinal)));
        }
    }

    public Task<IReadOnlyList<WalletLinkTokenRecord>> ListActiveByCardIdAsync(
        Guid cardId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var tokens = _store.WalletLinkTokens
                .Where(token =>
                    token.IsActive &&
                    token.CardId == cardId &&
                    string.Equals(token.Purpose, purpose, StringComparison.Ordinal))
                .OrderByDescending(token => token.CreatedAt)
                .ToArray();

            return Task.FromResult<IReadOnlyList<WalletLinkTokenRecord>>(tokens);
        }
    }

    public Task MarkUsedAsync(
        string tokenHash,
        string purpose,
        DateTimeOffset usedAt,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var index = _store.WalletLinkTokens.FindIndex(token =>
                token.IsActive &&
                string.Equals(token.TokenHash, tokenHash, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(token.Purpose, purpose, StringComparison.Ordinal));

            if (index >= 0)
            {
                _store.WalletLinkTokens[index] = _store.WalletLinkTokens[index] with { LastUsedAt = usedAt };
            }
        }

        return Task.CompletedTask;
    }

    public Task RevokeActiveByCardIdAsync(
        Guid cardId,
        string purpose,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            for (var index = 0; index < _store.WalletLinkTokens.Count; index++)
            {
                var token = _store.WalletLinkTokens[index];
                if (token.IsActive &&
                    token.CardId == cardId &&
                    string.Equals(token.Purpose, purpose, StringComparison.Ordinal))
                {
                    _store.WalletLinkTokens[index] = token with { RevokedAt = revokedAt };
                }
            }
        }

        return Task.CompletedTask;
    }
}
