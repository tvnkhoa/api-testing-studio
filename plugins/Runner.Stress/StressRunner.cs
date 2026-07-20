using System.Diagnostics;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Plugin.Abstractions.Runners;

namespace ApiTestingStudio.Runner.Stress;

/// <summary>
/// Default <see cref="IStressRunner"/>. Runs an optional warm-up, dispatches to the strategy for the
/// configured <see cref="StressMode"/>, aggregates samples into a bounded histogram, and streams live
/// snapshots to <see cref="IProgress{T}"/> on a fixed cadence. Cancellation stops issuing work and
/// finalizes the metrics over what completed rather than throwing.
/// </summary>
public sealed class StressRunner : IStressRunner
{
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(250);

    public async Task<StressRunResult> RunAsync(
        StressRunConfig config,
        Func<CancellationToken, Task<StressSample>> workload,
        IProgress<StressMetricsSnapshot>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(workload);

        await WarmUpAsync(config, workload, cancellationToken).ConfigureAwait(false);

        var aggregator = new MetricsAggregator();
        var strategy = CreateStrategy(config.Mode);
        var stopwatch = Stopwatch.StartNew();

        using var pumpCts = new CancellationTokenSource();
        var pump = progress is null
            ? Task.CompletedTask
            : PumpProgressAsync(progress, aggregator, stopwatch, pumpCts.Token);

        try
        {
            await strategy.ExecuteAsync(config, workload, aggregator, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            stopwatch.Stop();
            await pumpCts.CancelAsync().ConfigureAwait(false);
            await pump.ConfigureAwait(false);
        }

        var metrics = aggregator.Snapshot(stopwatch.Elapsed);
        progress?.Report(metrics);

        return new StressRunResult
        {
            Config = config,
            Metrics = metrics,
            Cancelled = cancellationToken.IsCancellationRequested,
        };
    }

    private static async Task WarmUpAsync(
        StressRunConfig config,
        Func<CancellationToken, Task<StressSample>> workload,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < config.WarmupIterations && !cancellationToken.IsCancellationRequested; i++)
        {
            try
            {
                _ = await workload(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private static async Task PumpProgressAsync(
        IProgress<StressMetricsSnapshot> progress,
        MetricsAggregator aggregator,
        Stopwatch stopwatch,
        CancellationToken pumpToken)
    {
        using var timer = new PeriodicTimer(ProgressInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(pumpToken).ConfigureAwait(false))
            {
                progress.Report(aggregator.Snapshot(stopwatch.Elapsed));
            }
        }
        catch (OperationCanceledException)
        {
            // Pump is stopped by cancelling its token once the run completes.
        }
    }

    private static IStressStrategy CreateStrategy(StressMode mode) => mode switch
    {
        StressMode.Sequential => new SequentialStrategy(),
        StressMode.Loop => new LoopStrategy(),
        StressMode.Concurrent => new ConcurrentStrategy(),
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown stress mode."),
    };
}
