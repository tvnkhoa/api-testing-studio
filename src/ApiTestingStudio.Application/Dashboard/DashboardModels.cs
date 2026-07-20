using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.Dashboard;

/// <summary>A point on the dashboard timeline: one completed run's latency at its completion time.</summary>
public sealed record TimelinePoint(DateTimeOffset TimestampUtc, string Label, double DurationMs, RunStatus Status);

/// <summary>An endpoint/workflow ranking row (slowest or most-called), grouped by target name.</summary>
public sealed record EndpointStat(string Name, int Count, double AverageMs);

/// <summary>A bucket in the error/status distribution.</summary>
public sealed record ErrorBucket(string Label, int Count);

/// <summary>
/// Immutable read-model surfaced to the Dashboard widgets (Sprint 13), aggregated from the unified run
/// store. The UI binds these; it holds no aggregation logic.
/// </summary>
public sealed record DashboardSnapshot
{
    public int RunCount { get; init; }

    public int PassedCount { get; init; }

    public int FailedCount { get; init; }

    public int CancelledCount { get; init; }

    /// <summary>Passed runs as a fraction of all runs (0..1).</summary>
    public double SuccessRate { get; init; }

    /// <summary>Failed runs as a fraction of all runs (0..1).</summary>
    public double FailureRate { get; init; }

    /// <summary>Mean run duration in milliseconds.</summary>
    public double AverageDurationMs { get; init; }

    public IReadOnlyList<TimelinePoint> Timeline { get; init; } = [];

    public IReadOnlyList<EndpointStat> SlowestTargets { get; init; } = [];

    public IReadOnlyList<EndpointStat> MostCalledTargets { get; init; } = [];

    public IReadOnlyList<ErrorBucket> StatusDistribution { get; init; } = [];

    public static DashboardSnapshot Empty { get; } = new();
}

/// <summary>Filters that drive the whole dashboard (Sprint 13).</summary>
public sealed record DashboardQuery
{
    /// <summary>Restrict to a single run source; null includes all sources.</summary>
    public RunSource? Source { get; init; }

    /// <summary>Maximum number of most-recent runs to aggregate over.</summary>
    public int MaxRuns { get; init; } = 200;

    /// <summary>Top-N size for the slowest / most-called rankings.</summary>
    public int TopN { get; init; } = 5;
}
