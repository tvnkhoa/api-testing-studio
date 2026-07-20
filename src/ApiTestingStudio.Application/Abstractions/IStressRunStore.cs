using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Durable store for completed stress runs (Sprint 12). A run is saved with its metric rows (a single
/// summary row today; time-series-ready). Reads are for the Dashboard (Sprint 13) to review past runs.
/// EF Core types stay in the Infrastructure implementation.
/// </summary>
public interface IStressRunStore
{
    /// <summary>Persists a run and its metric rows atomically.</summary>
    Task SaveAsync(StressRun run, IReadOnlyList<StressMetrics> metrics, CancellationToken cancellationToken = default);

    /// <summary>Returns the runs recorded in a workspace, most recent first.</summary>
    Task<IReadOnlyList<StressRun>> ListAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>Returns the metric rows for a run, ordered by <see cref="StressMetrics.SequenceIndex"/>.</summary>
    Task<IReadOnlyList<StressMetrics>> GetMetricsAsync(Guid stressRunId, CancellationToken cancellationToken = default);
}
