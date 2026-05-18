using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Domain;
using DigitalCards.Infrastructure.LegacySync;
using Microsoft.Extensions.Logging.Abstractions;

namespace DigitalCards.Application.Tests;

public sealed class LegacyWalletSyncProcessorTests
{
    [Fact]
    public void LegacyWalletSyncState_RecordsSafeRunState()
    {
        var state = new LegacyWalletSyncState();
        var startedAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        var completedAt = DateTimeOffset.UtcNow;

        state.RecordStarted(startedAt);
        state.RecordCompleted(completedAt, new LegacyWalletSyncRunResult(3, 2, 1, 0));

        var snapshot = state.Snapshot(enabled: true);

        Assert.True(snapshot.Enabled);
        Assert.Equal(startedAt, snapshot.LastStartedAt);
        Assert.Equal(completedAt, snapshot.LastCompletedAt);
        Assert.Equal(new LegacyWalletSyncRunResult(3, 2, 1, 0), snapshot.LastResult);
        Assert.Null(snapshot.LastErrorSummary);

        state.RecordFailed(completedAt.AddSeconds(1), new InvalidOperationException("secret detail"));
        var failed = state.Snapshot(enabled: true);

        Assert.Equal("InvalidOperationException", failed.LastErrorSummary);
        Assert.DoesNotContain("secret detail", failed.LastErrorSummary);
    }

    [Fact]
    public async Task SyncAsync_PatchesWalletsOncePerChangedFingerprint()
    {
        var card = CreateCard(googleObjectId: "google-object");
        var candidate = new LegacyWalletSyncCandidate(
            card,
            CreateClient(card.ClientId),
            CreateBusiness(card.BusinessId),
            HasRegisteredAppleDevices: true);
        var repository = new FakeLegacyWalletSyncRepository(candidate);
        var google = new RecordingGoogleWalletService();
        var apple = new RecordingAppleWalletService();
        var ledger = new RecordingStampLedgerRepository();
        var processor = new LegacyWalletSyncProcessor(
            repository,
            google,
            apple,
            ledger,
            new FixedClock(),
            NullLogger<LegacyWalletSyncProcessor>.Instance);
        var fingerprints = new Dictionary<Guid, string>();

        var first = await processor.SyncAsync(DateTimeOffset.UtcNow.AddHours(-1), 10, fingerprints);
        var second = await processor.SyncAsync(DateTimeOffset.UtcNow.AddHours(-1), 10, fingerprints);

        Assert.Equal(new LegacyWalletSyncRunResult(1, 1, 0, 0), first);
        Assert.Equal(new LegacyWalletSyncRunResult(1, 0, 1, 0), second);
        Assert.Equal(1, google.PatchCount);
        Assert.Equal(1, apple.NotifyCount);
        var record = Assert.Single(ledger.Records);
        Assert.Equal(StampLedgerSource.LegacySync, record.Source);
        Assert.Equal(card.Id, record.CardId);
        Assert.True(record.GoogleWalletAttempted);
        Assert.True(record.GoogleWalletSucceeded);
        Assert.True(record.AppleWalletAttempted);
        Assert.True(record.AppleWalletSucceeded);
    }

    [Fact]
    public async Task SyncAsync_ContinuesAfterCandidateFailure()
    {
        var failingCard = CreateCard(googleObjectId: "google-fails");
        var appleCard = CreateCard(googleObjectId: null, id: Guid.Parse("00000000-0000-0000-0000-000000000124"));
        var repository = new FakeLegacyWalletSyncRepository(
            new LegacyWalletSyncCandidate(
                failingCard,
                CreateClient(failingCard.ClientId),
                CreateBusiness(failingCard.BusinessId),
                HasRegisteredAppleDevices: false),
            new LegacyWalletSyncCandidate(
                appleCard,
                CreateClient(appleCard.ClientId),
                CreateBusiness(appleCard.BusinessId),
                HasRegisteredAppleDevices: true));
        var google = new RecordingGoogleWalletService(failPatch: true);
        var apple = new RecordingAppleWalletService();
        var ledger = new RecordingStampLedgerRepository();
        var processor = new LegacyWalletSyncProcessor(
            repository,
            google,
            apple,
            ledger,
            new FixedClock(),
            NullLogger<LegacyWalletSyncProcessor>.Instance);

        var result = await processor.SyncAsync(
            DateTimeOffset.UtcNow.AddHours(-1),
            10,
            new Dictionary<Guid, string>());

        Assert.Equal(2, result.Candidates);
        Assert.Equal(1, result.Synced);
        Assert.Equal(1, result.Failed);
        Assert.Equal(1, google.PatchCount);
        Assert.Equal(1, apple.NotifyCount);
        Assert.Equal(2, ledger.Records.Count);
        Assert.Equal("InvalidOperationException", ledger.Records[0].ErrorSummary);
        Assert.True(ledger.Records[0].GoogleWalletAttempted);
        Assert.False(ledger.Records[0].GoogleWalletSucceeded);
        Assert.True(ledger.Records[1].AppleWalletAttempted);
        Assert.True(ledger.Records[1].AppleWalletSucceeded);
    }

    private static LoyaltyCard CreateCard(string? googleObjectId, Guid? id = null)
    {
        var cardId = id ?? Guid.Parse("00000000-0000-0000-0000-000000000123");
        return LoyaltyCard.Restore(
            cardId,
            Guid.Parse("00000000-0000-0000-0000-000000000011"),
            Guid.Parse("00000000-0000-0000-0000-000000000022"),
            cardId.ToString("N"),
            currentStamps: 3,
            lifetimeStamps: 7,
            DateTimeOffset.Parse("2026-05-11T20:00:00Z"),
            DateTimeOffset.Parse("2026-05-11T21:00:00Z"),
            googleObjectId,
            googleSaveUrl: null);
    }

    private static Client CreateClient(Guid clientId)
    {
        return new Client(clientId, "legacy-client", "Legacy", "Client", "legacy@example.test");
    }

    private static Business CreateBusiness(Guid businessId)
    {
        return new Business(businessId, "Legacy Business", "legacy-business@example.test", "hash", "logo.png");
    }

    private sealed class FakeLegacyWalletSyncRepository : ILegacyWalletSyncRepository
    {
        private readonly IReadOnlyList<LegacyWalletSyncCandidate> _candidates;

        public FakeLegacyWalletSyncRepository(params LegacyWalletSyncCandidate[] candidates)
        {
            _candidates = candidates;
        }

        public Task<IReadOnlyList<LegacyWalletSyncCandidate>> ListCandidatesAsync(
            DateTimeOffset changedSince,
            int batchSize,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_candidates);
        }
    }

    private sealed class RecordingStampLedgerRepository : IStampLedgerRepository
    {
        public List<StampLedgerRecord> Records { get; } = [];

        public Task AddAsync(StampLedgerRecord record, CancellationToken cancellationToken = default)
        {
            Records.Add(record with { Id = Records.Count + 1 });
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<StampLedgerRecord>> ListRecentByCardIdAsync(
            Guid cardId,
            int limit,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<StampLedgerRecord>>(
                Records
                    .Where(record => record.CardId == cardId)
                    .OrderByDescending(record => record.CreatedAt)
                    .Take(limit)
                    .ToArray());
        }

        public Task<IReadOnlyList<StampLedgerRecord>> ListByBusinessAsync(
            Guid businessId,
            int limit,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<StampLedgerRecord>>(
                Records
                    .Where(record => record.BusinessId == businessId)
                    .OrderByDescending(record => record.CreatedAt)
                    .Take(limit)
                    .ToArray());
        }
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-05-11T22:00:00Z");
    }

    private sealed class RecordingGoogleWalletService : IGoogleWalletService
    {
        private readonly bool _failPatch;

        public RecordingGoogleWalletService(bool failPatch = false)
        {
            _failPatch = failPatch;
        }

        public int PatchCount { get; private set; }

        public Task<GoogleWalletIssueResult> IssueSaveLinkAsync(
            LoyaltyCard card,
            Client client,
            Business business,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task PatchStampStateAsync(
            LoyaltyCard card,
            Client client,
            Business business,
            CancellationToken cancellationToken = default)
        {
            PatchCount++;
            return _failPatch
                ? throw new InvalidOperationException("Patch failed.")
                : Task.CompletedTask;
        }
    }

    private sealed class RecordingAppleWalletService : IAppleWalletService
    {
        public int NotifyCount { get; private set; }

        public Task<AppleWalletIssueResult> IssueAsync(
            LoyaltyCard card,
            Client client,
            Business business,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AppleWalletPassFile> CreatePassAsync(
            LoyaltyCard card,
            Client client,
            Business business,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AppleWalletPassRequestResult> CreateUpdatedPassAsync(
            string passTypeIdentifier,
            string serialNumber,
            string? authorizationHeader,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AppleWalletRegistrationStatus> RegisterDeviceAsync(
            string deviceLibraryIdentifier,
            string passTypeIdentifier,
            string serialNumber,
            string pushToken,
            string? authorizationHeader,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AppleWalletUnregistrationStatus> UnregisterDeviceAsync(
            string deviceLibraryIdentifier,
            string passTypeIdentifier,
            string serialNumber,
            string? authorizationHeader,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AppleWalletUpdatedPasses?> ListUpdatedPassesAsync(
            string deviceLibraryIdentifier,
            string passTypeIdentifier,
            string? previousLastUpdated,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task NotifyPassUpdatedAsync(
            LoyaltyCard card,
            Client client,
            Business business,
            CancellationToken cancellationToken = default)
        {
            NotifyCount++;
            return Task.CompletedTask;
        }
    }
}
