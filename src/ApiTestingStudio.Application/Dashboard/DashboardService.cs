using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Runs;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Dashboard;

/// <summary>
/// Default <see cref="IDashboardService"/>. Aggregates run headers from <see cref="IRunStore"/> — cheap
/// enough to recompute on every run-completed notification without loading per-step detail. Grouping and
/// ranking run in memory over the capped most-recent window.
/// </summary>
public sealed class DashboardService : IDashboardService
{
    private readonly IRunStore _runStore;
    private readonly IWorkspaceSession _session;

    public DashboardService(IRunStore runStore, IWorkspaceSession session)
    {
        _runStore = runStore ?? throw new ArgumentNullException(nameof(runStore));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public async Task<Result<DashboardSnapshot>> GetSnapshotAsync(DashboardQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (_session.Current is not { } workspace)
        {
            return Result.Failure<DashboardSnapshot>(RunErrors.NoWorkspaceOpen);
        }

        var all = await _runStore.ListAsync(workspace.Id, cancellationToken).ConfigureAwait(false);

        var runs = all
            .Where(r => query.Source is null || r.Source == query.Source)
            .Take(Math.Max(1, query.MaxRuns))
            .ToList();

        if (runs.Count == 0)
        {
            return Result.Success(DashboardSnapshot.Empty);
        }

        var passed = runs.Count(r => r.Status == RunStatus.Passed);
        var failed = runs.Count(r => r.Status == RunStatus.Failed);
        var cancelled = runs.Count(r => r.Status == RunStatus.Cancelled);
        var total = runs.Count;

        var snapshot = new DashboardSnapshot
        {
            RunCount = total,
            PassedCount = passed,
            FailedCount = failed,
            CancelledCount = cancelled,
            SuccessRate = (double)passed / total,
            FailureRate = (double)failed / total,
            AverageDurationMs = runs.Average(r => r.DurationMs),
            Timeline = BuildTimeline(runs),
            SlowestTargets = RankTargets(runs, byLatency: true, query.TopN),
            MostCalledTargets = RankTargets(runs, byLatency: false, query.TopN),
            StatusDistribution = BuildStatusDistribution(passed, failed, cancelled),
        };

        return Result.Success(snapshot);
    }

    private static List<TimelinePoint> BuildTimeline(IReadOnlyList<Run> runs) =>
        runs
            .OrderBy(r => r.CompletedUtc ?? r.StartedUtc)
            .Select(r => new TimelinePoint(
                r.CompletedUtc ?? r.StartedUtc,
                r.TargetName,
                r.DurationMs,
                r.Status))
            .ToList();

    private static List<EndpointStat> RankTargets(IReadOnlyList<Run> runs, bool byLatency, int topN)
    {
        var grouped = runs
            .GroupBy(r => string.IsNullOrEmpty(r.TargetName) ? "(unnamed)" : r.TargetName)
            .Select(g => new EndpointStat(g.Key, g.Count(), g.Average(r => r.DurationMs)));

        grouped = byLatency
            ? grouped.OrderByDescending(s => s.AverageMs)
            : grouped.OrderByDescending(s => s.Count);

        return grouped.Take(Math.Max(1, topN)).ToList();
    }

    private static List<ErrorBucket> BuildStatusDistribution(int passed, int failed, int cancelled)
    {
        var buckets = new List<ErrorBucket>();
        if (passed > 0)
        {
            buckets.Add(new ErrorBucket("Passed", passed));
        }

        if (failed > 0)
        {
            buckets.Add(new ErrorBucket("Failed", failed));
        }

        if (cancelled > 0)
        {
            buckets.Add(new ErrorBucket("Cancelled", cancelled));
        }

        return buckets;
    }
}
