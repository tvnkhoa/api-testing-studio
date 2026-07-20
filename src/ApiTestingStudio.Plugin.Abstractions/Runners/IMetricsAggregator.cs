namespace ApiTestingStudio.Plugin.Abstractions.Runners;

/// <summary>
/// Accumulates <see cref="StressSample"/>s and produces <see cref="StressMetricsSnapshot"/>s.
/// Implementations use a low-overhead streaming approach (e.g. a bounded latency histogram) so they
/// never store every sample. The contract lives here so the Dashboard (Sprint 13) can reuse the same
/// aggregation abstraction. Implementations must be safe for concurrent <see cref="Record"/> calls.
/// </summary>
public interface IMetricsAggregator
{
    /// <summary>Records one workload outcome.</summary>
    void Record(StressSample sample);

    /// <summary>
    /// Produces a snapshot of the metrics accumulated so far, computing throughput over
    /// <paramref name="elapsed"/>.
    /// </summary>
    StressMetricsSnapshot Snapshot(TimeSpan elapsed);
}
