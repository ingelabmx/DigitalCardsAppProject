namespace DigitalCards.Infrastructure.Stripe;

public sealed class StripeOptions
{
    public const string SectionName = "DigitalCards:Stripe";
    public string SecretKey { get; init; } = "";
    public string WebhookSecret { get; init; } = "";
    public Dictionary<string, StripePlanOptions> Plans { get; init; } = new();
}

public sealed class StripePlanOptions
{
    public string PriceId { get; init; } = "";
    public string Name { get; init; } = "";
    public int MaxClients { get; init; } = 300;
}
