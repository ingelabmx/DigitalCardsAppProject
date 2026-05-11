namespace DigitalCards.Infrastructure.Wallets;

public sealed class AppleWalletOptions
{
    public const string SectionName = $"{DigitalCardsInfrastructureOptions.SectionName}:AppleWallet";

    public string? Provider { get; init; }
}
