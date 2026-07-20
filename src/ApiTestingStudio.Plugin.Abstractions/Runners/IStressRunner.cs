namespace ApiTestingStudio.Plugin.Abstractions.Runners;

/// <summary>
/// Drives a workload under load per a <see cref="StressRunConfig"/> and aggregates performance
/// metrics. Implemented by <c>Runner.Stress</c> and loaded as a plugin (capability
/// <see cref="PluginCapability.StressRunner"/>).
///
/// <para>The runner is deliberately independent of what it drives: the caller supplies a
/// <paramref name="workload"/> delegate that performs one invocation and returns a
/// <see cref="StressSample"/>. The Application orchestrator wires this to the request executor
/// (Sprint 06) or workflow engine (Sprint 08), so the same runner stresses a request, an endpoint,
/// or a workflow.</para>
/// </summary>
public interface IStressRunner
{
    /// <summary>
    /// Runs the workload under the given load shape. Each measured sample is fed to the aggregator and,
    /// when <paramref name="progress"/> is supplied, a live <see cref="StressMetricsSnapshot"/> is
    /// reported periodically so the UI can reflect throughput/latency as the run proceeds. Cancellation
    /// stops issuing new work promptly, finalizes the metrics over what completed, and returns a result
    /// with <see cref="StressRunResult.Cancelled"/> set — it does not throw
    /// <see cref="OperationCanceledException"/>.
    /// </summary>
    /// <param name="config">The load shape (mode, virtual users, iterations/duration, warm-up).</param>
    /// <param name="workload">Performs one invocation and returns its outcome.</param>
    /// <param name="progress">Optional live metrics sink.</param>
    /// <param name="cancellationToken">Stops the run cleanly and finalizes partial metrics.</param>
    Task<StressRunResult> RunAsync(
        StressRunConfig config,
        Func<CancellationToken, Task<StressSample>> workload,
        IProgress<StressMetricsSnapshot>? progress = null,
        CancellationToken cancellationToken = default);
}
