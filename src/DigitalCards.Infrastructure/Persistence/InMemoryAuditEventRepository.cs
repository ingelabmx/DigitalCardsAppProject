using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryAuditEventRepository : IAuditEventRepository
{
    private readonly InMemoryDigitalCardsStore _store;

    public InMemoryAuditEventRepository(InMemoryDigitalCardsStore store)
    {
        _store = store;
    }

    public Task AddAsync(OperationalAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var id = auditEvent.Id > 0 ? auditEvent.Id : _store.AuditEvents.Count + 1;
            _store.AuditEvents.Add(auditEvent with { Id = id });
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OperationalAuditEvent>> ListRecentAsync(
        OperationalAuditEventType? eventType,
        string? search,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var normalizedSearch = search?.Trim();
            var query = _store.AuditEvents.AsEnumerable();

            if (eventType is not null)
            {
                query = query.Where(item => item.EventType == eventType);
            }

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                query = query.Where(item =>
                    item.Summary.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                    item.EventType.ToString().Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                    item.BusinessId?.ToString("N").Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true ||
                    item.ClientId?.ToString("N").Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true ||
                    item.CardId?.ToString("N").Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true);
            }

            if (from is not null)
            {
                query = query.Where(item => item.CreatedAt >= from.Value);
            }

            if (to is not null)
            {
                query = query.Where(item => item.CreatedAt <= to.Value);
            }

            var records = query
                .OrderByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.Id)
                .Take(Math.Max(1, Math.Min(limit, 250)))
                .ToArray();

            return Task.FromResult<IReadOnlyList<OperationalAuditEvent>>(records);
        }
    }
}
