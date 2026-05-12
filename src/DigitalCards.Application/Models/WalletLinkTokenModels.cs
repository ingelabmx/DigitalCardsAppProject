namespace DigitalCards.Application.Models;

public static class WalletLinkPurposes
{
    public const string WalletSelect = "WalletSelect";
}

public sealed record WalletLinkTokenRecord(
    Guid CardId,
    string Purpose,
    string TokenHash,
    string TokenSuffix,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset? RevokedAt)
{
    public bool IsActive => RevokedAt is null;
}
