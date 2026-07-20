using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Domain.Entities;

/// <summary>
/// The persisted root row of a visual workflow (the <c>Workflows</c> table). The node/edge graph
/// lives in the sibling <see cref="WorkflowNode"/> / <see cref="WorkflowEdge"/> tables, keyed by
/// <c>WorkflowId</c>; the repository assembles them into a runtime <see cref="Workflow"/> aggregate.
/// </summary>
public sealed record WorkflowDefinition
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkspaceId { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }
}

/// <summary>
/// One node in a workflow graph. <see cref="Config"/> is a node-kind-specific JSON payload
/// (<c>System.Text.Json</c>); the engine's handler for <see cref="Kind"/> interprets it.
/// <see cref="PositionX"/>/<see cref="PositionY"/> are canvas coordinates for the Sprint 09 designer;
/// <see cref="Width"/>/<see cref="Height"/>/<see cref="Color"/> are optional visual metadata for it.
/// </summary>
public sealed record WorkflowNode
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkflowId { get; init; }

    public WorkflowNodeKind Kind { get; init; }

    public required string Name { get; init; }

    public double PositionX { get; init; }

    public double PositionY { get; init; }

    /// <summary>Designer node width in canvas units; null lets the view choose a default (Sprint 09).</summary>
    public double? Width { get; init; }

    /// <summary>Designer node height in canvas units; null lets the view choose a default (Sprint 09).</summary>
    public double? Height { get; init; }

    /// <summary>Optional designer accent colour (e.g. a hex string); null uses the kind's default (Sprint 09).</summary>
    public string? Color { get; init; }

    /// <summary>Node-kind-specific configuration as JSON, or null when the node needs none.</summary>
    public string? Config { get; init; }
}

/// <summary>
/// A directed connection between two nodes. <see cref="SourcePort"/> distinguishes branch outputs
/// (e.g. a Condition node's <c>true</c>/<c>false</c> ports); <see cref="Mapping"/> carries an
/// optional data-mapping expression resolved by the variable substitution engine.
/// </summary>
public sealed record WorkflowEdge
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkflowId { get; init; }

    public Guid SourceNodeId { get; init; }

    public Guid TargetNodeId { get; init; }

    public string? SourcePort { get; init; }

    public string? TargetPort { get; init; }

    /// <summary>Optional data-mapping expression (e.g. <c>{{Login.token}}</c>).</summary>
    public string? Mapping { get; init; }
}

/// <summary>
/// The runtime workflow aggregate the engine executes: a <see cref="WorkflowDefinition"/> root
/// hydrated with its full node/edge graph. Assembled by <c>IWorkflowRepository</c>; it is not a
/// table of its own.
/// </summary>
public sealed record Workflow
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkspaceId { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public IReadOnlyList<WorkflowNode> Nodes { get; init; } = [];

    public IReadOnlyList<WorkflowEdge> Edges { get; init; } = [];
}

/// <summary>
/// A named grouping of <see cref="TestCaseDefinition"/>s (the <c>TestSuites</c> table). A suite is
/// executed as a unit and reports aggregate pass/fail across its cases.
/// </summary>
public sealed record TestSuite
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkspaceId { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    /// <summary>Ordering among sibling suites in the workspace.</summary>
    public int SortOrder { get; init; }
}

/// <summary>
/// A test case that executes a target — either a single endpoint request
/// (<see cref="EndpointId"/>) or a workflow (<see cref="WorkflowId"/>) — and evaluates its
/// <see cref="AssertionDefinition"/>s to Pass/Fail. Exactly one of <see cref="EndpointId"/> /
/// <see cref="WorkflowId"/> is set. The <c>TestCases</c> table.
/// </summary>
public sealed record TestCaseDefinition
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkspaceId { get; init; }

    /// <summary>Owning suite, or null for an ungrouped case.</summary>
    public Guid? TestSuiteId { get; init; }

    /// <summary>The endpoint this case sends a single request against; null for a workflow case.</summary>
    public Guid? EndpointId { get; init; }

    /// <summary>The workflow this case runs; null for a single-request case.</summary>
    public Guid? WorkflowId { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    /// <summary>Ordering among sibling cases within a suite.</summary>
    public int SortOrder { get; init; }
}

/// <summary>
/// The runtime test-case aggregate: a <see cref="TestCaseDefinition"/> hydrated with its ordered
/// <see cref="AssertionDefinition"/>s. Assembled by the repository; not a table of its own (mirrors
/// the <see cref="Workflow"/> aggregate).
/// </summary>
public sealed record TestCase
{
    public required TestCaseDefinition Definition { get; init; }

    public IReadOnlyList<AssertionDefinition> Assertions { get; init; } = [];
}

/// <summary>
/// A persisted assertion attached to a <see cref="TestCaseDefinition"/> (the <c>Assertions</c>
/// table). <see cref="Kind"/> selects the assertion plugin ("json"/"regex"/"schema");
/// <see cref="Source"/> (+ optional <see cref="Target"/>) selects which response field becomes the
/// actual value; <see cref="Expression"/> carries the kind-specific selector (JSONPath / regex
/// pattern) and <see cref="Operator"/> the comparison.
/// </summary>
public sealed record AssertionDefinition
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid TestCaseId { get; init; }

    /// <summary>Assertion plugin kind: "json", "regex", or "schema".</summary>
    public required string Kind { get; init; }

    /// <summary>Which part of the response to evaluate.</summary>
    public AssertionSource Source { get; init; }

    /// <summary>Header name when <see cref="Source"/> is <see cref="AssertionSource.Header"/>; else null.</summary>
    public string? Target { get; init; }

    /// <summary>Kind-specific selector (JSONPath for "json", regex pattern for "regex"); null when unused.</summary>
    public string? Expression { get; init; }

    /// <summary>Comparison operator for value assertions (e.g. "equals", "contains"); null when unused.</summary>
    public string? Operator { get; init; }

    /// <summary>Expected value / schema text compared against the actual.</summary>
    public string Expected { get; init; } = string.Empty;

    /// <summary>Whether this assertion is evaluated (disabled ones are skipped).</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Ordering among sibling assertions within a case.</summary>
    public int SortOrder { get; init; }
}

/// <summary>
/// The runtime outcome of evaluating one <see cref="AssertionDefinition"/>. Not a table of its own;
/// serialized into <see cref="TestRunResult.DetailsJson"/>.
/// </summary>
public sealed record AssertionResult
{
    public Guid AssertionId { get; init; }

    /// <summary>The assertion kind that was evaluated.</summary>
    public required string Kind { get; init; }

    public AssertionOutcome Outcome { get; init; }

    /// <summary>Human-readable reason/diff explaining the outcome; null when none.</summary>
    public string? Message { get; init; }
}

/// <summary>
/// The persisted result of executing one <see cref="TestCaseDefinition"/> (the <c>TestResults</c>
/// table). Denormalized aggregate columns support cheap list rendering; <see cref="DetailsJson"/>
/// holds the per-assertion <see cref="AssertionResult"/> array (<c>System.Text.Json</c>), mirroring
/// the <see cref="RequestHistoryEntry"/> snapshot pattern.
/// </summary>
public sealed record TestRunResult
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkspaceId { get; init; }

    public Guid TestCaseId { get; init; }

    /// <summary>Owning suite at run time, or null for an ungrouped case.</summary>
    public Guid? TestSuiteId { get; init; }

    public RunStatus Status { get; init; }

    public int PassedCount { get; init; }

    public int FailedCount { get; init; }

    public int SkippedCount { get; init; }

    public long DurationMs { get; init; }

    public DateTimeOffset TimestampUtc { get; init; }

    /// <summary>Serialized <see cref="AssertionResult"/> array for this run.</summary>
    public string DetailsJson { get; init; } = "[]";
}
