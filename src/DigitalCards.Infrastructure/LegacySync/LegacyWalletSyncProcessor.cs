using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using Microsoft.Extensions.Logging;

namespace DigitalCards.Infrastructure.LegacySync;

public sealed class LegacyWalletSyncProcessor
{
    private readonly IAppleWalletService _appleWallet;
    private readonly IGoogleWalletService _googleWallet;
    private readonly ILegacyWalletSyncRepository _legacySync;
    private readonly ILogger<LegacyWalletSyncProcessor> _logger;

    public LegacyWalletSyncProcessor(
        ILegacyWalletSyncRepository legacySync,
        IGoogleWalletService googleWallet,
        IAppleWalletService appleWallet,
        ILogger<LegacyWalletSyncProcessor> logger)
    {
        _legacySync = legacySync;
        _googleWallet = googleWallet;
        _appleWallet = appleWallet;
        _logger = logger;
    }

    public async Task<LegacyWalletSyncRunResult> SyncAsync(
        DateTimeOffset changedSince,
        int batchSize,
        IDictionary<Guid, string> syncedFingerprints,
        CancellationToken cancellationToken = default)
    {
        var candidates = await _legacySync.ListCandidatesAsync(changedSince, batchSize, cancellationToken);
        var synced = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var candidate in candidates)
        {
            var fingerprint = CreateFingerprint(candidate);
            if (syncedFingerprints.TryGetValue(candidate.Card.Id, out var previousFingerprint) &&
                string.Equals(previousFingerprint, fingerprint, StringComparison.Ordinal))
            {
                skipped++;
                continue;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(candidate.Card.GoogleObjectId))
                {
                    await _googleWallet.PatchStampStateAsync(
                        candidate.Card,
                        candidate.Client,
                        candidate.Business,
                        cancellationToken);
                }

                if (candidate.HasRegisteredAppleDevices)
                {
                    await _appleWallet.NotifyPassUpdatedAsync(
                        candidate.Card,
                        candidate.Client,
                        candidate.Business,
                        cancellationToken);
                }

                syncedFingerprints[candidate.Card.Id] = fingerprint;
                synced++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failed++;
                _logger.LogWarning(
                    exception,
                    "Legacy Wallet sync failed for card {CardId}.",
                    candidate.Card.Id);
            }
        }

        return new LegacyWalletSyncRunResult(candidates.Count, synced, skipped, failed);
    }

    private static string CreateFingerprint(LegacyWalletSyncCandidate candidate)
    {
        var card = candidate.Card;
        return string.Join(
            "|",
            card.CurrentStamps,
            card.LifetimeStamps,
            card.LastStampedAt.ToUnixTimeMilliseconds(),
            card.GoogleObjectId ?? string.Empty,
            candidate.HasRegisteredAppleDevices ? "apple" : string.Empty);
    }
}
