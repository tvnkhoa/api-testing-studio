using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.Workflows;

/// <summary>
/// Tunable policies for a single workflow run. All values have safe defaults so callers can pass
/// <see cref="Default"/> (or nothing) for the common case.
/// </summary>
public sealed record WorkflowRunOptions
{
    /// <summary>How the engine reacts when a node fails. Defaults to aborting the run.</summary>
    public NodeFailurePolicy FailurePolicy { get; init; } = NodeFailurePolicy.StopOnError;

    /// <summary>
    /// Per-node timeout in milliseconds applied to leaf nodes (0 disables it). Container nodes
    /// (Loop/Parallel/Delay) are exempt because they legitimately run long; their inner nodes still
    /// get this timeout.
    /// </summary>
    public int DefaultNodeTimeoutMs { get; init; } = 100_000;

    /// <summary>Hard cap on loop iterations so a runaway collection/count cannot hang the engine.</summary>
    public int MaxLoopIterations { get; init; } = 10_000;

    /// <summary>Default bound on concurrent branches for a Parallel node.</summary>
    public int DefaultMaxDegreeOfParallelism { get; init; } = 4;

    /// <summary>Shared default instance.</summary>
    public static WorkflowRunOptions Default { get; } = new();
}
