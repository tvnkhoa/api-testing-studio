using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Durable store for the unified run-log tree (Sprint 13): a <see cref="Run"/> header plus its
/// <see cref="RunStep"/> rows, written by request/workflow/stress executions and read by the
/// Dashboard, the execution Timeline, and Replay. EF Core types stay in the Infrastructure
/// implementation; operations require an open workspace. See <c>.claude/FEATURES/Logging.md</c>.
/// </summary>
public interface IRunStore
{
    /// <summary>Persists a run and its step rows atomically.</summary>
    Task SaveAsync(Run run, IReadOnlyList<RunStep> steps, CancellationToken cancellationToken = default);

    /// <summary>Returns the runs recorded in a workspace, most recent first.</summary>
    Task<IReadOnlyList<Run>> ListAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>Returns a single run header, or null when it does not exist.</summary>
    Task<Run?> GetAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>Returns the steps for a run, ordered by <see cref="RunStep.Order"/> within each scope.</summary>
    Task<IReadOnlyList<RunStep>> GetStepsAsync(Guid runId, CancellationToken cancellationToken = default);
}
