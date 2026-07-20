using ApiTestingStudio.Plugin.Abstractions.Runners;

namespace ApiTestingStudio.Runner.Stress;

/// <summary>
/// <see cref="StressRunConfig.VirtualUsers"/> workers issue the workload concurrently. The run is
/// bounded by <see cref="StressRunConfig.Duration"/> when set, otherwise by the shared
/// <see cref="StressRunConfig.Iterations"/> budget. Concurrency is bounded by the fixed worker count
/// (no unbounded task fan-out).
/// </summary>
internal sealed class ConcurrentStrategy : IStressStrategy
{
    public async Task ExecuteAsync(
        StressRunConfig config,
        Func<CancellationToken, Task<StressSample>> workload,
        IMetricsAggregator aggregator,
        CancellationToken cancellationToken)
    {
        var virtualUsers = Math.Max(1, config.VirtualUsers);

        using var runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (config.Duration is { } duration && duration > TimeSpan.Zero)
        {
            runCts.CancelAfter(duration);
        }

        // Duration-bounded runs have no request cap; iteration-bounded runs share this budget.
        var budget = config.Duration is null && config.Iterations > 0 ? config.Iterations : long.MaxValue;
        var issued = 0L;
        var token = runCts.Token;

        var workers = new Task[virtualUsers];
        for (var i = 0; i < virtualUsers; i++)
        {
            workers[i] = RunWorkerAsync();
        }

        await Task.WhenAll(workers).ConfigureAwait(false);

        async Task RunWorkerAsync()
        {
            while (!token.IsCancellationRequested)
            {
                // Claim a slot from the shared budget before doing work so the cap is exact.
                if (Interlocked.Increment(ref issued) > budget)
                {
                    break;
                }

                try
                {
                    aggregator.Record(await workload(token).ConfigureAwait(false));
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
