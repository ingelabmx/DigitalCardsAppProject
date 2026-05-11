namespace DigitalCards.Infrastructure.Wallets;

public sealed class AppleWalletOptions
{
    public const string SectionName = $"{DigitalCardsInfrastructureOptions.SectionName}:AppleWallet";

    public string? Provider { get; init; }

    public string? TeamIdentifier { get; init; }

    public string? PassTypeIdentifier { get; init; }

    public string? OrganizationName { get; init; }

    public string Description { get; init; } = "Digital loyalty card";

    public string? CertificatePath { get; init; }

    public string? CertificatePassword { get; init; }

    public string? WwdrCertificatePath { get; init; }

    public string? AssetsPath { get; init; }

    public string? AuthenticationTokenSecret { get; init; }

    public string ApnsBaseUrl { get; init; } = "https://api.push.apple.com";

    public string BackgroundColor { get; init; } = "rgb(31, 105, 169)";

    public string ForegroundColor { get; init; } = "rgb(255, 255, 255)";

    public string LabelColor { get; init; } = "rgb(214, 232, 247)";
}
