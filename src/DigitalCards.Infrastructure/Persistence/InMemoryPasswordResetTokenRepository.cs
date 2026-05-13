using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryPasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly InMemoryDigitalCardsStore _store;

    public InMemoryPasswordResetTokenRepository(InMemoryDigitalCardsStore store)
    {
        _store = store;
    }

    public Task AddAsync(
        PasswordResetTokenRecord token,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var id = token.Id > 0
                ? token.Id
                : (_store.PasswordResetTokens.Count == 0 ? 1 : _store.PasswordResetTokens.Max(existing => existing.Id) + 1);
            _store.PasswordResetTokens.Add(token with { Id = id });
        }

        return Task.CompletedTask;
    }

    public Task<PasswordResetTokenRecord?> FindActiveByTokenHashAsync(
        string tokenHash,
        PasswordResetAccountType accountType,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            return Task.FromResult(_store.PasswordResetTokens.SingleOrDefault(token =>
                token.AccountType == accountType &&
                string.Equals(token.TokenHash, tokenHash, StringComparison.OrdinalIgnoreCase) &&
                token.IsActive(now)));
        }
    }

    public Task MarkUsedAsync(
        long id,
        DateTimeOffset usedAt,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var index = _store.PasswordResetTokens.FindIndex(token => token.Id == id);
            if (index >= 0)
            {
                _store.PasswordResetTokens[index] = _store.PasswordResetTokens[index] with { UsedAt = usedAt };
            }
        }

        return Task.CompletedTask;
    }

    public Task RevokeActiveByAccountAsync(
        PasswordResetAccountType accountType,
        Guid accountId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            for (var index = 0; index < _store.PasswordResetTokens.Count; index++)
            {
                var token = _store.PasswordResetTokens[index];
                if (token.AccountType == accountType &&
                    token.AccountId == accountId &&
                    token.UsedAt is null &&
                    token.RevokedAt is null)
                {
                    _store.PasswordResetTokens[index] = token with { RevokedAt = revokedAt };
                }
            }
        }

        return Task.CompletedTask;
    }
}
