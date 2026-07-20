using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Stress;

/// <summary>Typed errors for stress orchestration, returned via <see cref="Result"/>.</summary>
public static class StressErrors
{
    public static Error NoWorkspaceOpen { get; } =
        new("stress.no_workspace", "No workspace is currently open.");

    public static Error RunnerUnavailable { get; } =
        new("stress.runner_unavailable", "No stress runner plugin is loaded.");

    public static Error NoTarget { get; } =
        new("stress.no_target", "The stress run has no target to drive.");
}
