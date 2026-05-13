namespace DigitalCards.Domain;

public sealed record OperationalAuditEvent(
    long Id,
    OperationalAuditEventType EventType,
    Guid ActorAdminUserId,
    Guid? BusinessId,
    Guid? ClientId,
    Guid? CardId,
    Guid? TargetAdminUserId,
    string Summary,
    DateTimeOffset CreatedAt);
