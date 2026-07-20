using ApiTestingStudio.Plugin.Abstractions.Runners;

namespace ApiTestingStudio.Runner.Stress;

/// <summary>
/// A load-issuing strategy for one <see cref="StressMode"/>. Drives the workload and feeds each
/// measured outcome to the aggregator; must observe cancellation promptly and stop issuing new work
/// without throwing (a cancelled in-flight workload surfaces as <see cref="OperationCanceledException"/>,
/// which the strategy swallows).
/// </summary>
internal interface IStressStrategy
{
    Task ExecuteAsync(
        StressRunConfig config,
        Func<CancellationToken, Task<StressSample>> workload,
        IMetricsAggregator aggregator,
        CancellationToken cancellationToken);
}
