namespace DigitalCards.Infrastructure.LegacySync;

public sealed class LegacyWalletSyncState
{
    private readonly object _gate = new();
    private DateTimeOffset? _lastStartedAt;
    private DateTimeOffset? _lastCompletedAt;
    private DateTimeOffset? _lastFailedAt;
    private LegacyWalletSyncRunResult? _lastResult;
    private string? _lastErrorSummary;

    public LegacyWalletSyncStateSnapshot Snapshot(bool enabled)
    {
        lock (_gate)
        {
            return new LegacyWalletSyncStateSnapshot(
                enabled,
                _lastStartedAt,
                _lastCompletedAt,
                _lastFailedAt,
                _lastResult,
                _lastErrorSummary);
        }
    }

    public void RecordStarted(DateTimeOffset startedAt)
    {
        lock (_gate)
        {
            _lastStartedAt = startedAt;
        }
    }

    public void RecordCompleted(DateTimeOffset completedAt, LegacyWalletSyncRunResult result)
    {
        lock (_gate)
        {
            _lastCompletedAt = completedAt;
            _lastResult = result;
            _lastErrorSummary = null;
        }
    }

    public void RecordFailed(DateTimeOffset failedAt, Exception exception)
    {
        lock (_gate)
        {
            _lastFailedAt = failedAt;
            _lastErrorSummary = exception.GetType().Name;
        }
    }
}

public sealed record LegacyWalletSyncStateSnapshot(
    bool Enabled,
    DateTimeOffset? LastStartedAt,
    DateTimeOffset? LastCompletedAt,
    DateTimeOffset? LastFailedAt,
    LegacyWalletSyncRunResult? LastResult,
    string? LastErrorSummary);
