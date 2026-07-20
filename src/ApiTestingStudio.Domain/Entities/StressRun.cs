using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Domain.Entities;

/// <summary>
/// A persisted record of one stress run (Sprint 12): the target it drove, the load configuration, and
/// a few headline metrics denormalized for cheap list rendering in the Dashboard (Sprint 13). The
/// full metric set is stored in the related <see cref="StressMetrics"/> row(s), keyed by
/// <see cref="Id"/>. Timestamps come from the app clock and are set by the orchestrator, not the
/// runner. See <c>.claude/DATABASE_GUIDELINES.md</c>.
/// </summary>
public sealed record StressRun
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkspaceId { get; init; }

    /// <summary>What the run drove: an ad-hoc request, a saved endpoint, or a workflow.</summary>
    public StressTargetKind TargetKind { get; init; }

    /// <summary>Endpoint or workflow id when <see cref="TargetKind"/> is not <see cref="StressTargetKind.Request"/>.</summary>
    public Guid? TargetId { get; init; }

    /// <summary>Human-readable target label captured at run time (method+url, endpoint, or workflow name).</summary>
    public required string TargetName { get; init; }

    public StressMode Mode { get; init; } = StressMode.Sequential;

    /// <summary>Configured concurrent virtual users (relevant for <see cref="StressMode.Concurrent"/>).</summary>
    public int VirtualUsers { get; init; }

    /// <summary>Configured iteration budget.</summary>
    public int Iterations { get; init; }

    /// <summary>Configured duration limit in milliseconds, or null when the run was iteration-bounded.</summary>
    public long? DurationMs { get; init; }

    /// <summary>Warm-up invocations excluded from the metrics.</summary>
    public int WarmupIterations { get; init; }

    /// <summary>True when the run was cancelled and its metrics were finalized over the partial run.</summary>
    public bool Cancelled { get; init; }

    public DateTimeOffset StartedUtc { get; init; }

    public DateTimeOffset CompletedUtc { get; init; }

    /// <summary>Total completed requests — denormalized headline metric.</summary>
    public long RequestCount { get; init; }

    /// <summary>Throughput (requests/second) — denormalized headline metric.</summary>
    public double RequestsPerSecond { get; init; }

    /// <summary>95th-percentile latency (ms) — denormalized headline metric.</summary>
    public double P95Ms { get; init; }

    /// <summary>Failure fraction (0..1) — denormalized headline metric.</summary>
    public double ErrorRate { get; init; }
}

/// <summary>
/// Metrics for a <see cref="StressRun"/>, keyed by <see cref="StressRunId"/>. Sprint 12 writes a
/// single final <em>summary</em> row (<see cref="SequenceIndex"/> = <c>-1</c>); the shape also admits
/// time-series snapshots (<see cref="SequenceIndex"/> ≥ 0) for a future live-history feature without a
/// schema change. Latency values are milliseconds; <see cref="ErrorRate"/> is a 0..1 fraction.
/// </summary>
public sealed record StressMetrics
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid StressRunId { get; init; }

    /// <summary><c>-1</c> for the final summary row; <c>≥ 0</c> for an ordered time-series snapshot.</summary>
    public int SequenceIndex { get; init; } = -1;

    /// <summary>Elapsed wall-clock time the metrics cover, milliseconds.</summary>
    public long ElapsedMs { get; init; }

    public long RequestCount { get; init; }

    public long FailureCount { get; init; }

    public double RequestsPerSecond { get; init; }

    public double MinMs { get; init; }

    public double AverageMs { get; init; }

    public double MaxMs { get; init; }

    public double P50Ms { get; init; }

    public double P95Ms { get; init; }

    public double P99Ms { get; init; }

    public double ErrorRate { get; init; }
}
