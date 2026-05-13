namespace DigitalCards.Web.Security;

public sealed class DigitalCardsSecurityOptions
{
    public const string SectionName = "DigitalCards:Security";

    public RateLimitOptions RateLimiting { get; set; } = new();

    public sealed class RateLimitOptions
    {
        public int AuthPermitLimit { get; set; } = 20;

        public int AuthWindowSeconds { get; set; } = 60;

        public int PublicWritePermitLimit { get; set; } = 30;

        public int PublicWriteWindowSeconds { get; set; } = 60;

        public int WalletPermitLimit { get; set; } = 300;

        public int WalletWindowSeconds { get; set; } = 60;
    }
}
