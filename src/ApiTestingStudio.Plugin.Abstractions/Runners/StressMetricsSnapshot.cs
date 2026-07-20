namespace ApiTestingStudio.Plugin.Abstractions.Runners;

/// <summary>
/// A point-in-time (or final) view of the metrics aggregated during a stress run: counts,
/// throughput, latency distribution, and error rate. Streamed live via
/// <see cref="System.IProgress{T}"/> during a run and returned inside <see cref="StressRunResult"/>
/// at the end. Latency values are milliseconds; <see cref="ErrorRate"/> is a 0..1 fraction.
/// </summary>
public sealed record StressMetricsSnapshot
{
    /// <summary>Wall-clock time elapsed since measurement began.</summary>
    public TimeSpan Elapsed { get; init; }

    /// <summary>Total workload invocations completed (successes plus failures).</summary>
    public long Completed { get; init; }

    /// <summary>Invocations that failed (transport error or unsuccessful status).</summary>
    public long Failed { get; init; }

    /// <summary>Throughput in completed requests per second over <see cref="Elapsed"/>.</summary>
    public double RequestsPerSecond { get; init; }

    /// <summary>Minimum observed latency, milliseconds.</summary>
    public double MinMs { get; init; }

    /// <summary>Mean observed latency, milliseconds.</summary>
    public double AverageMs { get; init; }

    /// <summary>Maximum observed latency, milliseconds.</summary>
    public double MaxMs { get; init; }

    /// <summary>50th-percentile (median) latency, milliseconds.</summary>
    public double P50Ms { get; init; }

    /// <summary>95th-percentile latency, milliseconds.</summary>
    public double P95Ms { get; init; }

    /// <summary>99th-percentile latency, milliseconds.</summary>
    public double P99Ms { get; init; }

    /// <summary>Fraction of completed invocations that failed, 0..1.</summary>
    public double ErrorRate { get; init; }

    /// <summary>An empty snapshot (all zeros), used as the initial live value.</summary>
    public static StressMetricsSnapshot Empty { get; } = new();
}
