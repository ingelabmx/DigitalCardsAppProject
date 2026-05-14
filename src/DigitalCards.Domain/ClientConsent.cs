namespace DigitalCards.Domain;

public sealed record ClientConsent(
    long Id,
    Guid ClientId,
    Guid? BusinessId,
    string PolicyVersion,
    string Source,
    DateTimeOffset AcceptedAt);
