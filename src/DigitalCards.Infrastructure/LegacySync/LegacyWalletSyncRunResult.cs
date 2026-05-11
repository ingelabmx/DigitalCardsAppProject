namespace DigitalCards.Infrastructure.LegacySync;

public sealed record LegacyWalletSyncRunResult(
    int Candidates,
    int Synced,
    int Skipped,
    int Failed);
