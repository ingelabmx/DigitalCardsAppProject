namespace DigitalCards.Domain;

public sealed record CutoverSmokeEvidence(
    long Id,
    Guid BusinessId,
    Guid AdminUserId,
    bool HealthOk,
    bool ReadyOk,
    bool EmailOk,
    bool AppleWalletOk,
    bool GoogleWalletOk,
    bool ModernStampOk,
    bool SupportReviewed,
    string? Notes,
    DateTimeOffset CreatedAt)
{
    public bool IsComplete =>
        HealthOk &&
        ReadyOk &&
        EmailOk &&
        AppleWalletOk &&
        GoogleWalletOk &&
        ModernStampOk &&
        SupportReviewed;
}
