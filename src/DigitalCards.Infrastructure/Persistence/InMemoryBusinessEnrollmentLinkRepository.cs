using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryBusinessEnrollmentLinkRepository : IBusinessEnrollmentLinkRepository
{
    private readonly InMemoryDigitalCardsStore _store;

    public InMemoryBusinessEnrollmentLinkRepository(InMemoryDigitalCardsStore store)
    {
        _store = store;
    }

    public Task AddAsync(BusinessEnrollmentLinkRecord token, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            _store.BusinessEnrollmentLinks.Add(token);
        }

        return Task.CompletedTask;
    }

    public Task<BusinessEnrollmentLinkRecord?> FindActiveByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            return Task.FromResult(_store.BusinessEnrollmentLinks.SingleOrDefault(token =>
                token.IsActive &&
                string.Equals(token.TokenHash, tokenHash, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<IReadOnlyList<BusinessEnrollmentLinkRecord>> ListActiveByBusinessIdAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var tokens = _store.BusinessEnrollmentLinks
                .Where(token => token.IsActive && token.BusinessId == businessId)
                .OrderByDescending(token => token.CreatedAt)
                .ToArray();

            return Task.FromResult<IReadOnlyList<BusinessEnrollmentLinkRecord>>(tokens);
        }
    }

    public Task MarkUsedAsync(
        string tokenHash,
        DateTimeOffset usedAt,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var index = _store.BusinessEnrollmentLinks.FindIndex(token =>
                token.IsActive &&
                string.Equals(token.TokenHash, tokenHash, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
            {
                _store.BusinessEnrollmentLinks[index] = _store.BusinessEnrollmentLinks[index] with
                {
                    LastUsedAt = usedAt
                };
            }
        }

        return Task.CompletedTask;
    }

    public Task RevokeActiveByBusinessIdAsync(
        Guid businessId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            for (var index = 0; index < _store.BusinessEnrollmentLinks.Count; index++)
            {
                var token = _store.BusinessEnrollmentLinks[index];
                if (token.IsActive && token.BusinessId == businessId)
                {
                    _store.BusinessEnrollmentLinks[index] = token with { RevokedAt = revokedAt };
                }
            }
        }

        return Task.CompletedTask;
    }
}
