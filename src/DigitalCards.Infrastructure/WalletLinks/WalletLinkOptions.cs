namespace DigitalCards.Infrastructure.WalletLinks;

public sealed class WalletLinkOptions
{
    public const string SectionName = "DigitalCards:WalletLinks";

    public bool AllowLegacyCardIdTokens { get; init; }
}
