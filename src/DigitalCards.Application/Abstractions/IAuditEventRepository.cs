using DigitalCards.Domain;

namespace DigitalCards.Application.Abstractions;

public interface IAuditEventRepository
{
    Task AddAsync(OperationalAuditEvent auditEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalAuditEvent>> ListRecentAsync(
        OperationalAuditEventType? eventType,
        string? search,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit,
        CancellationToken cancellationToken = default);
}
