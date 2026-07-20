namespace ApiTestingStudio.Plugin.Abstractions.Runners;

/// <summary>
/// The final result of a stress run: the config that produced it, the aggregated metrics, and
/// whether it was cancelled (in which case the metrics are finalized over the partial run). Wall-clock
/// timestamps and the target identity are added by the Application orchestrator when persisting, so
/// the runner itself stays free of clock/target concerns.
/// </summary>
public sealed record StressRunResult
{
    /// <summary>The configuration the run executed.</summary>
    public required StressRunConfig Config { get; init; }

    /// <summary>Final aggregated metrics (finalized even when <see cref="Cancelled"/> is true).</summary>
    public required StressMetricsSnapshot Metrics { get; init; }

    /// <summary>True when the run stopped early due to cancellation.</summary>
    public bool Cancelled { get; init; }
}
