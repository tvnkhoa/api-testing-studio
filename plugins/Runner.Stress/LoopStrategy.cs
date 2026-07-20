using ApiTestingStudio.Plugin.Abstractions.Runners;

namespace ApiTestingStudio.Runner.Stress;

/// <summary><see cref="StressRunConfig.Iterations"/> passes back-to-back, one request at a time.</summary>
internal sealed class LoopStrategy : IStressStrategy
{
    public async Task ExecuteAsync(
        StressRunConfig config,
        Func<CancellationToken, Task<StressSample>> workload,
        IMetricsAggregator aggregator,
        CancellationToken cancellationToken)
    {
        var iterations = Math.Max(1, config.Iterations);
        for (var i = 0; i < iterations && !cancellationToken.IsCancellationRequested; i++)
        {
            try
            {
                aggregator.Record(await workload(cancellationToken).ConfigureAwait(false));
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
