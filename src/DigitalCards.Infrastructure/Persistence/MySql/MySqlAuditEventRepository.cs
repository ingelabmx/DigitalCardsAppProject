using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;
using MySqlConnector;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlAuditEventRepository : IAuditEventRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlAuditEventRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(OperationalAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into ModernAuditEvent (
                EventType,
                ActorAdminUserID,
                BusinessID,
                UserID,
                CardID,
                TargetAdminUserID,
                Summary,
                CreatedAt)
            values (
                @EventType,
                @ActorAdminUserID,
                @BusinessID,
                @UserID,
                @CardID,
                @TargetAdminUserID,
                @Summary,
                @CreatedAt);
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            await connection.ExecuteAsync(new CommandDefinition(
                sql,
                ToParameters(auditEvent),
                cancellationToken: cancellationToken));
        }
        catch (MySqlException exception) when (exception.Number == 1146)
        {
            throw MissingTableException(exception);
        }
    }

    public async Task<IReadOnlyList<OperationalAuditEvent>> ListRecentAsync(
        OperationalAuditEventType? eventType,
        string? search,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select ID,
                   EventType,
                   ActorAdminUserID,
                   BusinessID,
                   UserID,
                   CardID,
                   TargetAdminUserID,
                   Summary,
                   CreatedAt
            from ModernAuditEvent
            where (@EventType is null or EventType = @EventType)
              and (@Search is null or Summary like @Search or EventType like @Search)
              and (@From is null or CreatedAt >= @From)
              and (@To is null or CreatedAt <= @To)
            order by CreatedAt desc, ID desc
            limit @Limit;
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            var rows = await connection.QueryAsync<AuditEventRow>(new CommandDefinition(
                sql,
                new
                {
                    EventType = eventType?.ToString(),
                    Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%",
                    From = from?.UtcDateTime,
                    To = to?.UtcDateTime,
                    Limit = Math.Max(1, Math.Min(limit, 250))
                },
                cancellationToken: cancellationToken));

            return rows.Select(row => row.ToDomain()).ToArray();
        }
        catch (MySqlException exception) when (exception.Number == 1146)
        {
            throw MissingTableException(exception);
        }
    }

    private static object ToParameters(OperationalAuditEvent auditEvent)
    {
        return new
        {
            EventType = auditEvent.EventType.ToString(),
            ActorAdminUserID = LegacyIdMapper.ToInt32(auditEvent.ActorAdminUserId),
            BusinessID = auditEvent.BusinessId is null ? (int?)null : LegacyIdMapper.ToInt32(auditEvent.BusinessId.Value),
            UserID = auditEvent.ClientId is null ? (int?)null : LegacyIdMapper.ToInt32(auditEvent.ClientId.Value),
            CardID = auditEvent.CardId is null ? (int?)null : LegacyIdMapper.ToInt32(auditEvent.CardId.Value),
            TargetAdminUserID = auditEvent.TargetAdminUserId is null ? (int?)null : LegacyIdMapper.ToInt32(auditEvent.TargetAdminUserId.Value),
            Summary = auditEvent.Summary,
            CreatedAt = auditEvent.CreatedAt.UtcDateTime
        };
    }

    private static InvalidOperationException MissingTableException(MySqlException exception)
    {
        return new InvalidOperationException(
            "ModernAuditEvent table is missing. Run docs/migration-context/66-operational-audit-log-hostgator.sql before using operational audit with MySQL.",
            exception);
    }

    private static DateTimeOffset AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc))
            : new DateTimeOffset(value.ToUniversalTime());
    }

    private sealed class AuditEventRow
    {
        public long ID { get; set; }

        public string EventType { get; set; } = string.Empty;

        public int ActorAdminUserID { get; set; }

        public int? BusinessID { get; set; }

        public int? UserID { get; set; }

        public int? CardID { get; set; }

        public int? TargetAdminUserID { get; set; }

        public string Summary { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public OperationalAuditEvent ToDomain()
        {
            return new OperationalAuditEvent(
                ID,
                Enum.TryParse<OperationalAuditEventType>(EventType, ignoreCase: true, out var eventType)
                    ? eventType
                    : OperationalAuditEventType.BusinessUpdated,
                LegacyIdMapper.ToGuid(ActorAdminUserID),
                BusinessID is null ? null : LegacyIdMapper.ToGuid(BusinessID.Value),
                UserID is null ? null : LegacyIdMapper.ToGuid(UserID.Value),
                CardID is null ? null : LegacyIdMapper.ToGuid(CardID.Value),
                TargetAdminUserID is null ? null : LegacyIdMapper.ToGuid(TargetAdminUserID.Value),
                Summary,
                AsUtc(CreatedAt));
        }
    }
}
