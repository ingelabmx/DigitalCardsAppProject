namespace DigitalCards.Infrastructure;

public sealed class DigitalCardsInfrastructureOptions
{
    public const string SectionName = "DigitalCards";

    public bool UseFakeIntegrations { get; init; } = true;
}

