using DigitalCards.Application.Models;

namespace DigitalCards.Application.Abstractions;

public interface IBusinessEnrollmentLinkRepository
{
    Task AddAsync(BusinessEnrollmentLinkRecord token, CancellationToken cancellationToken = default);

    Task<BusinessEnrollmentLinkRecord?> FindActiveByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BusinessEnrollmentLinkRecord>> ListActiveByBusinessIdAsync(
        Guid businessId,
        CancellationToken cancellationToken = default);

    Task MarkUsedAsync(
        string tokenHash,
        DateTimeOffset usedAt,
        CancellationToken cancellationToken = default);

    Task RevokeActiveByBusinessIdAsync(
        Guid businessId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken = default);
}
