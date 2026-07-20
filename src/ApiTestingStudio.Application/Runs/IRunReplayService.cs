using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Runs;

/// <summary>The outcome of replaying a recorded run: re-executing its request(s) or workflow.</summary>
public sealed record RunReplayResult
{
    /// <summary>Overall status of the replay.</summary>
    public RunStatus Status { get; init; }

    /// <summary>Number of steps that were re-executed.</summary>
    public int StepCount { get; init; }

    /// <summary>Human-readable summary of the replay outcome.</summary>
    public required string Summary { get; init; }
}

/// <summary>
/// Re-executes a previously recorded run (Sprint 13). Request runs re-drive their captured request
/// snapshots through the executor; workflow runs re-run the workflow by id. Stress runs are not
/// replayable. Reconstructing a run for read-only walking is done against <c>IRunStore</c> directly.
/// </summary>
public interface IRunReplayService
{
    Task<Result<RunReplayResult>> ReplayAsync(Guid runId, CancellationToken cancellationToken = default);
}
