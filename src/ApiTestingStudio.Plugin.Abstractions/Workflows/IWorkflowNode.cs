using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Plugin.Abstractions.Workflows;

/// <summary>Mutable execution context passed between workflow nodes (variables, outputs).</summary>
public sealed record NodeExecutionContext(IReadOnlyDictionary<string, string> Variables);

/// <summary>The result a node returns after executing.</summary>
public sealed record NodeResult(bool Succeeded, IReadOnlyDictionary<string, string> Outputs, string? Message = null)
{
    public static NodeResult Ok(IReadOnlyDictionary<string, string>? outputs = null) =>
        new(true, outputs ?? new Dictionary<string, string>());
}

/// <summary>
/// Executes one kind of workflow node (API, Condition, Loop, Delay, Parallel, Switch,
/// Variable, Assertion). Implemented by workflow plugins.
/// </summary>
public interface IWorkflowNode
{
    /// <summary>The node kind this implementation handles.</summary>
    WorkflowNodeKind Kind { get; }

    /// <summary>Executes the node against the current context.</summary>
    Task<NodeResult> ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default);
}
