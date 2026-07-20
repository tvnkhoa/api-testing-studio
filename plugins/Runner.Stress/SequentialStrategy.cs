using ApiTestingStudio.Plugin.Abstractions.Runners;

namespace ApiTestingStudio.Runner.Stress;

/// <summary>A single pass of the workload, one request at a time.</summary>
internal sealed class SequentialStrategy : IStressStrategy
{
    public async Task ExecuteAsync(
        StressRunConfig config,
        Func<CancellationToken, Task<StressSample>> workload,
        IMetricsAggregator aggregator,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            aggregator.Record(await workload(cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            // Cancellation finalizes over what completed; nothing to record for the aborted call.
        }
    }
}
