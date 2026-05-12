using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using Microsoft.Extensions.Logging;

namespace DigitalCards.Infrastructure.LegacySync;

public sealed class LegacyWalletSyncProcessor
{
    private readonly IAppleWalletService _appleWallet;
    private readonly IClock _clock;
    private readonly IGoogleWalletService _googleWallet;
    private readonly ILegacyWalletSyncRepository _legacySync;
    private readonly ILogger<LegacyWalletSyncProcessor> _logger;
    private readonly IStampLedgerRepository _stampLedger;

    public LegacyWalletSyncProcessor(
        ILegacyWalletSyncRepository legacySync,
        IGoogleWalletService googleWallet,
        IAppleWalletService appleWallet,
        IStampLedgerRepository stampLedger,
        IClock clock,
        ILogger<LegacyWalletSyncProcessor> logger)
    {
        _legacySync = legacySync;
        _googleWallet = googleWallet;
        _appleWallet = appleWallet;
        _stampLedger = stampLedger;
        _clock = clock;
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

            var googleAttempted = false;
            var googleSucceeded = false;
            var appleAttempted = false;
            var appleSucceeded = false;

            try
            {
                if (!string.IsNullOrWhiteSpace(candidate.Card.GoogleObjectId))
                {
                    googleAttempted = true;
                    await _googleWallet.PatchStampStateAsync(
                        candidate.Card,
                        candidate.Client,
                        candidate.Business,
                        cancellationToken);
                    googleSucceeded = true;
                }

                if (candidate.HasRegisteredAppleDevices)
                {
                    appleAttempted = true;
                    await _appleWallet.NotifyPassUpdatedAsync(
                        candidate.Card,
                        candidate.Client,
                        candidate.Business,
                        cancellationToken);
                    appleSucceeded = true;
                }

                await RecordLedgerAsync(
                    candidate,
                    googleAttempted,
                    googleSucceeded,
                    appleAttempted,
                    appleSucceeded,
                    errorSummary: null,
                    cancellationToken);

                syncedFingerprints[candidate.Card.Id] = fingerprint;
                synced++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                await RecordLedgerAsync(
                    candidate,
                    googleAttempted,
                    googleSucceeded,
                    appleAttempted,
                    appleSucceeded,
                    SafeErrorSummary(exception),
                    cancellationToken);

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

    private async Task RecordLedgerAsync(
        LegacyWalletSyncCandidate candidate,
        bool googleAttempted,
        bool googleSucceeded,
        bool appleAttempted,
        bool appleSucceeded,
        string? errorSummary,
        CancellationToken cancellationToken)
    {
        var card = candidate.Card;
        await _stampLedger.AddAsync(
            new StampLedgerRecord(
                0,
                card.Id,
                card.BusinessId,
                card.ClientId,
                StampLedgerSource.LegacySync,
                null,
                card.CurrentStamps,
                card.CurrentStamps,
                card.LifetimeStamps,
                card.LifetimeStamps,
                card.LastStampedAt,
                googleAttempted,
                googleSucceeded,
                appleAttempted,
                appleSucceeded,
                errorSummary,
                _clock.UtcNow),
            cancellationToken);
    }

    private static string SafeErrorSummary(Exception exception)
    {
        return exception.GetType().Name;
    }
}
