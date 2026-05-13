namespace DigitalCards.Application.Models;

public enum PasswordResetAccountType
{
    Client,
    Business
}

public sealed record PasswordResetTokenRecord(
    long Id,
    PasswordResetAccountType AccountType,
    Guid AccountId,
    string TokenHash,
    string TokenSuffix,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? UsedAt,
    DateTimeOffset? RevokedAt)
{
    public bool IsActive(DateTimeOffset now)
    {
        return UsedAt is null && RevokedAt is null && ExpiresAt > now;
    }
}
