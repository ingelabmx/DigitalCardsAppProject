using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalCards.Infrastructure.LegacySync;

public sealed class LegacyWalletSyncWorker : BackgroundService
{
    private readonly IOptions<LegacyWalletSyncOptions> _options;
    private readonly ILogger<LegacyWalletSyncWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly LegacyWalletSyncState _state;
    private readonly Dictionary<Guid, string> _syncedFingerprints = new();

    public LegacyWalletSyncWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<LegacyWalletSyncOptions> options,
        LegacyWalletSyncState state,
        ILogger<LegacyWalletSyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _state = state;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;
        var interval = TimeSpan.FromSeconds(Math.Max(1, options.PollIntervalSeconds));

        await RunOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;
        var changedSince = DateTimeOffset.UtcNow.AddMinutes(-Math.Max(1, options.LookbackMinutes));
        var batchSize = Math.Max(1, options.BatchSize);
        _state.RecordStarted(DateTimeOffset.UtcNow);

        LegacyWalletSyncRunResult result;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<LegacyWalletSyncProcessor>();
            result = await processor.SyncAsync(changedSince, batchSize, _syncedFingerprints, cancellationToken);
            _state.RecordCompleted(DateTimeOffset.UtcNow, result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _state.RecordFailed(DateTimeOffset.UtcNow, exception);
            throw;
        }

        if (result.Candidates == 0 && result.Failed == 0)
        {
            return;
        }

        _logger.LogInformation(
            "Legacy Wallet sync scanned {Candidates} candidates, synced {Synced}, skipped {Skipped}, failed {Failed}.",
            result.Candidates,
            result.Synced,
            result.Skipped,
            result.Failed);
    }
}
