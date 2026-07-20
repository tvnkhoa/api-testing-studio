namespace ApiTestingStudio.Runner.Stress;

/// <summary>
/// Tracks the completed-request count and derives throughput (requests/second) over an elapsed
/// window. Kept a distinct component so throughput logic stays separable from latency aggregation and
/// can evolve (e.g. sliding-window instantaneous rate) without touching <see cref="MetricsAggregator"/>.
/// Not thread-safe; <see cref="MetricsAggregator"/> serializes access.
/// </summary>
internal sealed class ThroughputMeter
{
    private long _completed;

    /// <summary>Total completions recorded so far.</summary>
    public long Completed => _completed;

    /// <summary>Records one completed request.</summary>
    public void Increment() => _completed++;

    /// <summary>Cumulative requests-per-second, or zero when no time has elapsed.</summary>
    public double RequestsPerSecond(TimeSpan elapsed)
    {
        var seconds = elapsed.TotalSeconds;
        return seconds > 0 ? _completed / seconds : 0;
    }
}
