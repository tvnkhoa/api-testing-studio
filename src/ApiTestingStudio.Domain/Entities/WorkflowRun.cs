using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Domain.Entities;

/// <summary>
/// The outcome of executing a single node during a workflow run: its status, any outputs it
/// published into the context, an error message when it failed, and how long it took.
/// <see cref="Iteration"/> is set for nodes produced inside a Loop or Parallel fan-out.
/// </summary>
public sealed record NodeRunResult
{
    public Guid NodeId { get; init; }

    public required string NodeName { get; init; }

    public WorkflowNodeKind Kind { get; init; }

    public RunStatus Status { get; init; } = RunStatus.Pending;

    /// <summary>Scalar outputs the node published into the workflow context.</summary>
    public IReadOnlyDictionary<string, string> Outputs { get; init; } =
        new Dictionary<string, string>();

    /// <summary>Failure message when <see cref="Status"/> is <see cref="RunStatus.Failed"/>.</summary>
    public string? Error { get; init; }

    /// <summary>
    /// Non-fatal warnings raised while executing the node — currently the <c>{{tokens}}</c> that
    /// could not be resolved and were substituted with empty strings. Empty when everything
    /// resolved. Surfaced so a hollow workflow run is never silently reported as a clean pass.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    public TimeSpan Duration { get; init; }

    /// <summary>Zero-based iteration index when this result came from a Loop/Parallel branch.</summary>
    public int? Iteration { get; init; }

    /// <summary>
    /// Nested results produced by a container node (Loop iterations / Parallel branches). Empty for
    /// leaf nodes.
    /// </summary>
    public IReadOnlyList<NodeRunResult> Children { get; init; } = [];
}

/// <summary>
/// The structured result of a whole workflow execution: overall status, per-node results in
/// execution order, total duration, and a top-level error when the run itself failed. Returned
/// in-memory by the engine (Sprint 08); persistence is deferred to Sprint 13.
/// </summary>
public sealed record WorkflowRunResult
{
    public Guid WorkflowId { get; init; }

    public RunStatus Status { get; init; } = RunStatus.Pending;

    public IReadOnlyList<NodeRunResult> Nodes { get; init; } = [];

    public TimeSpan Duration { get; init; }

    public string? Error { get; init; }
}
