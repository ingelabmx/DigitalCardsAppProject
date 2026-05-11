namespace DigitalCards.Infrastructure.LegacySync;

public sealed class LegacyWalletSyncOptions
{
    public const string SectionName = $"{DigitalCardsInfrastructureOptions.SectionName}:LegacyWalletSync";

    public bool Enabled { get; init; }

    public int PollIntervalSeconds { get; init; } = 60;

    public int LookbackMinutes { get; init; } = 1440;

    public int BatchSize { get; init; } = 50;
}
