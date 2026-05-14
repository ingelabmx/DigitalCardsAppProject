namespace DigitalCards.Application.Models;

public sealed record RecordClientConsentCommand(
    Guid ClientId,
    Guid? BusinessId,
    string PolicyVersion,
    string Source);
