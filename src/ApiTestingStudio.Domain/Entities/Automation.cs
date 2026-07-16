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

/// <summary>A test case that runs a workflow and evaluates assertions to Pass/Fail.</summary>
public sealed record TestCaseDefinition
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkspaceId { get; init; }

    public Guid WorkflowId { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }
}
