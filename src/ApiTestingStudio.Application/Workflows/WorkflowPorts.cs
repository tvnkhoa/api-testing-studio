using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.Workflows;

/// <summary>
/// The canonical port names used across the workflow graph. Centralized here so the engine, the
/// connector validator, and the designer's graph mapper all speak the same vocabulary and can never
/// drift (an edge the designer draws is always one the engine knows how to traverse).
/// </summary>
public static class WorkflowPorts
{
    /// <summary>The single input port every node exposes (an edge's target side).</summary>
    public const string In = "in";

    /// <summary>The default continuation output the engine follows after a node.</summary>
    public const string Next = "next";

    /// <summary>A <see cref="WorkflowNodeKind.Condition"/> node's truthy branch output.</summary>
    public const string True = "true";

    /// <summary>A <see cref="WorkflowNodeKind.Condition"/> node's falsy branch output.</summary>
    public const string False = "false";

    /// <summary>A container node's (Loop/Parallel) inner-branch output; driven by its handler, not the walk.</summary>
    public const string Body = "body";

    /// <summary>The output key a Condition node publishes its boolean outcome under (read by the engine).</summary>
    public const string ConditionResultKey = "result";
}

/// <summary>
/// Describes the input/output ports a node exposes for a given <see cref="WorkflowNodeKind"/>. Single
/// source of truth for both the designer (which ports to render / allow wiring) and the connector
/// validator (whether a drawn edge references a real port).
/// </summary>
public static class NodePortCatalog
{
    /// <summary>Every node kind exposes a single input port; the entry node simply has no incoming edge.</summary>
    public static IReadOnlyList<string> InputPorts(WorkflowNodeKind kind) => [WorkflowPorts.In];

    /// <summary>
    /// Output ports for a kind: Condition branches on <c>true</c>/<c>false</c>; container kinds
    /// (Loop/Parallel) expose a <c>body</c> inner branch plus a <c>next</c> continuation; everything
    /// else has a single <c>next</c> continuation.
    /// </summary>
    public static IReadOnlyList<string> OutputPorts(WorkflowNodeKind kind) => kind switch
    {
        WorkflowNodeKind.Condition => [WorkflowPorts.True, WorkflowPorts.False],
        WorkflowNodeKind.Loop or WorkflowNodeKind.Parallel => [WorkflowPorts.Body, WorkflowPorts.Next],
        _ => [WorkflowPorts.Next],
    };
}
