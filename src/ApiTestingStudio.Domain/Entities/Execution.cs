using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Domain.Entities;

/// <summary>A single execution of a workflow or test case; the root of the run log tree.</summary>
public sealed record Run
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkspaceId { get; init; }

    public Guid? WorkflowId { get; init; }

    public RunStatus Status { get; init; } = RunStatus.Pending;

    public DateTimeOffset StartedUtc { get; init; }

    public DateTimeOffset? CompletedUtc { get; init; }
}

/// <summary>One ordered step within a <see cref="Run"/>.</summary>
public sealed record RunStep
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid RunId { get; init; }

    public int Order { get; init; }

    public required string Name { get; init; }

    public RunStatus Status { get; init; } = RunStatus.Pending;
}

/// <summary>A structured log line captured during execution (replayable).</summary>
public sealed record LogEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkspaceId { get; init; }

    public Guid? RunId { get; init; }

    public required string Level { get; init; }

    public required string Message { get; init; }

    public DateTimeOffset TimestampUtc { get; init; }
}
