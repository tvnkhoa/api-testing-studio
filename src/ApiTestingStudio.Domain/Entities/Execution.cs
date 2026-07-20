using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Domain.Entities;

/// <summary>
/// A single execution of a request, workflow, or stress run — the root of the persisted run-log tree
/// (Sprint 13). One unified store backs the Dashboard, the execution Timeline, and Replay; the
/// <see cref="Source"/> discriminates what produced the run. Timestamps come from the app clock and
/// are set by the writer, not the transport. See <c>.claude/FEATURES/Logging.md</c>.
/// </summary>
public sealed record Run
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkspaceId { get; init; }

    /// <summary>What produced the run (request / workflow / stress).</summary>
    public RunSource Source { get; init; } = RunSource.Request;

    /// <summary>The workflow id when <see cref="Source"/> is <see cref="RunSource.Workflow"/>; otherwise null.</summary>
    public Guid? WorkflowId { get; init; }

    /// <summary>Endpoint / workflow / stress target id captured at run time (null for ad-hoc requests).</summary>
    public Guid? TargetId { get; init; }

    /// <summary>Human-readable target label captured at run time (method+url, endpoint, or workflow name).</summary>
    public string TargetName { get; init; } = string.Empty;

    public RunStatus Status { get; init; } = RunStatus.Pending;

    public DateTimeOffset StartedUtc { get; init; }

    public DateTimeOffset? CompletedUtc { get; init; }

    /// <summary>Total wall-clock duration in milliseconds, denormalized for cheap list rendering.</summary>
    public long DurationMs { get; init; }

    /// <summary>Top-level failure message when the run itself failed; otherwise null.</summary>
    public string? Error { get; init; }
}

/// <summary>
/// One step within a <see cref="Run"/>. Steps form a tree via <see cref="ParentStepId"/> so Loop and
/// Parallel fan-outs nest under their container; leaf request steps carry the request/response JSON
/// snapshots needed for Replay and the status/timing needed for Dashboard aggregation.
/// </summary>
public sealed record RunStep
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid RunId { get; init; }

    /// <summary>Parent step for nested Loop/Parallel results; null for top-level steps.</summary>
    public Guid? ParentStepId { get; init; }

    /// <summary>Execution order within the parent scope (top-level order when <see cref="ParentStepId"/> is null).</summary>
    public int Order { get; init; }

    public required string Name { get; init; }

    /// <summary>The step kind (workflow node kind, or <c>"Request"</c> for request/stress steps).</summary>
    public string Kind { get; init; } = string.Empty;

    public RunStatus Status { get; init; } = RunStatus.Pending;

    /// <summary>Response status code for request steps; null for non-request steps.</summary>
    public int? StatusCode { get; init; }

    /// <summary>Step duration in milliseconds.</summary>
    public long DurationMs { get; init; }

    public DateTimeOffset? StartedUtc { get; init; }

    /// <summary>Zero-based iteration index when this step came from a Loop/Parallel branch.</summary>
    public int? Iteration { get; init; }

    /// <summary>Failure message when <see cref="Status"/> is <see cref="RunStatus.Failed"/>.</summary>
    public string? Error { get; init; }

    /// <summary>JSON snapshot of the <c>HttpRequestModel</c> sent (request steps only), enabling Replay.</summary>
    public string? RequestSnapshot { get; init; }

    /// <summary>JSON snapshot of the <c>HttpResponseModel</c> received (request steps only).</summary>
    public string? ResponseSnapshot { get; init; }
}

/// <summary>
/// An application (Serilog) log event persisted to the open workspace's database (Sprint 13). This is
/// the <em>application</em> log the Log Viewer reads — distinct from <see cref="LogEntry"/>, which is
/// the <em>execution</em> log attached to a run. Secrets are never written here (see
/// <c>.claude/FEATURES/Profiles.md</c>).
/// </summary>
public sealed record LogEventRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkspaceId { get; init; }

    public DateTimeOffset TimestampUtc { get; init; }

    /// <summary>Serilog level name (Verbose/Debug/Information/Warning/Error/Fatal).</summary>
    public required string Level { get; init; }

    /// <summary>The log source (Serilog <c>SourceContext</c> / logger category), for source filtering.</summary>
    public string Source { get; init; } = string.Empty;

    public required string Message { get; init; }

    /// <summary>Rendered exception detail when present; otherwise null.</summary>
    public string? Exception { get; init; }

    /// <summary>Optional JSON of structured properties for the event.</summary>
    public string? Properties { get; init; }
}

/// <summary>A structured log line captured during execution (replayable), attached to a run/step.</summary>
public sealed record LogEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkspaceId { get; init; }

    public Guid? RunId { get; init; }

    public required string Level { get; init; }

    public required string Message { get; init; }

    public DateTimeOffset TimestampUtc { get; init; }
}
