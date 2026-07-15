using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.Workflows;

/// <summary>
/// Executes a sub-branch of the graph starting at <paramref name="startNodeId"/> and returns the
/// per-node results. Provided by the engine to container handlers (Loop/Parallel) so they can drive
/// their body without the engine needing to know about specific node kinds.
/// </summary>
public delegate Task<IReadOnlyList<NodeRunResult>> BranchExecutor(
    Guid startNodeId,
    IWorkflowContext context,
    CancellationToken cancellationToken);

/// <summary>Everything a node handler needs to execute one node.</summary>
public sealed class NodeHandlerContext
{
    /// <summary>The node being executed.</summary>
    public required WorkflowNode Node { get; init; }

    /// <summary>The workflow the node belongs to (for graph lookups by container nodes).</summary>
    public required Workflow Workflow { get; init; }

    /// <summary>Shared, mutable run context (variables + node outputs).</summary>
    public required IWorkflowContext Context { get; init; }

    /// <summary>Token/expression resolver.</summary>
    public required IVariableResolver Resolver { get; init; }

    /// <summary>Run-wide policies (timeouts, loop cap, parallelism).</summary>
    public required WorkflowRunOptions Options { get; init; }

    /// <summary>Runs a child branch; used by Loop/Parallel nodes.</summary>
    public required BranchExecutor RunBranch { get; init; }
}

/// <summary>
/// Engine-side strategy for one <see cref="WorkflowNodeKind"/>. Built-in this sprint; node kinds may
/// become plugin-contributed later. Distinct from the plugin-facing <c>IWorkflowNode</c> contract.
/// </summary>
public interface INodeHandler
{
    /// <summary>The node kind this handler executes.</summary>
    WorkflowNodeKind Kind { get; }

    /// <summary>Executes the node against the current context and returns its result.</summary>
    Task<NodeRunResult> ExecuteAsync(NodeHandlerContext context, CancellationToken cancellationToken = default);
}
