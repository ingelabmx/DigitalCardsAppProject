using DigitalCards.Domain;

namespace DigitalCards.Application.Models;

public sealed record AdminAuditQuery(
    OperationalAuditEventType? EventType = null,
    string? Search = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Limit = 100);

public sealed record AdminAuditEventDto(
    long Id,
    OperationalAuditEventType EventType,
    Guid ActorAdminUserId,
    string ActorAdminLabel,
    Guid? BusinessId,
    string? BusinessLabel,
    Guid? ClientId,
    string? ClientLabel,
    Guid? CardId,
    Guid? TargetAdminUserId,
    string? TargetAdminLabel,
    string Summary,
    DateTimeOffset CreatedAt);

public sealed record RecordSupportExportAuditCommand(
    Guid AdminUserId,
    string ExportType,
    string Query,
    int CardCount);

public sealed record RecordBusinessEnrollmentLinkAuditCommand(
    Guid AdminUserId,
    Guid BusinessId);
