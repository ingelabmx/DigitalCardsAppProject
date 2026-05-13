using DigitalCards.Application.Models;

namespace DigitalCards.Application.Abstractions;

public interface IPasswordResetTokenRepository
{
    Task AddAsync(
        PasswordResetTokenRecord token,
        CancellationToken cancellationToken = default);

    Task<PasswordResetTokenRecord?> FindActiveByTokenHashAsync(
        string tokenHash,
        PasswordResetAccountType accountType,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task MarkUsedAsync(
        long id,
        DateTimeOffset usedAt,
        CancellationToken cancellationToken = default);

    Task RevokeActiveByAccountAsync(
        PasswordResetAccountType accountType,
        Guid accountId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken = default);
}
