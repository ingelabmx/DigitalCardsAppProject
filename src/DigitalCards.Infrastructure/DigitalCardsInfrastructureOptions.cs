namespace DigitalCards.Infrastructure;

public sealed class DigitalCardsInfrastructureOptions
{
    public const string SectionName = "DigitalCards";

    public bool UseFakeIntegrations { get; init; } = true;

    public string PersistenceProvider { get; init; } = "InMemory";

    public string? PublicBaseUrl { get; init; }
}
