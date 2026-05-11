namespace DigitalCards.Infrastructure.Wallets;

public sealed class GoogleWalletOptions
{
    public const string SectionName = $"{DigitalCardsInfrastructureOptions.SectionName}:GoogleWallet";

    public string? Provider { get; init; }

    public string? IssuerId { get; init; }

    public string? CredentialsFilePath { get; init; }

    public string ApplicationName { get; init; } = "DigitalCardsApp";

    public string[] Origins { get; init; } = [];

    public string Language { get; init; } = "en-US";

    public string HexBackgroundColor { get; init; } = "#ACD2E8";

    public string? HeroImageUri { get; init; } =
        "https://drive.google.com/uc?export=view&id=1AQ6mabyWRiBf58gNTu2C6mEsb_863z5u";

    public string? LogoImageUri { get; init; } =
        "https://drive.usercontent.google.com/download?id=1UV3C6c4vzWbnHwsiLp0tBAryx7PBu8K6";
}
