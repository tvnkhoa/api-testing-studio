using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Runs;

/// <summary>Typed errors for the run store / replay use cases (Sprint 13).</summary>
public static class RunErrors
{
    public static Error NoWorkspaceOpen { get; } =
        new("run.no_workspace", "No workspace is open.");

    public static Error NotFound(Guid runId) =>
        new("run.not_found", $"Run '{runId}' was not found.");

    public static Error NotReplayable { get; } =
        new("run.not_replayable", "This run has no replayable steps.");

    public static Error StressReplayUnsupported { get; } =
        new("run.stress_replay_unsupported", "Stress runs cannot be replayed; start a new stress run instead.");
}
