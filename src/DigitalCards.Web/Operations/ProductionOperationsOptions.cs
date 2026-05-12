namespace DigitalCards.Web.Operations;

public sealed class ProductionOperationsOptions
{
    public const string SectionName = "DigitalCards:Operations";

    public bool EnableForwardedHeaders { get; set; } = true;

    public bool TrustAllForwardedHeaders { get; set; }

    public string[] KnownProxies { get; set; } = [];

    public string? DataProtectionKeysPath { get; set; }

    public bool RequireDataProtectionKeysForReadiness { get; set; }
}
