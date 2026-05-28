namespace DigitalCards.Application.Models;

public sealed record BusinessEnrollmentLinkRecord(
    Guid BusinessId,
    string TokenHash,
    string TokenSuffix,
    string Token,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset? RevokedAt)
{
    public bool IsActive => RevokedAt is null;
}
