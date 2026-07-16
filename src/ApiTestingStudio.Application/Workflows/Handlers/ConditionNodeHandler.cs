using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.Workflows.Handlers;

/// <summary>
/// Executes a <see cref="WorkflowNodeKind.Condition"/> node: resolves both operands (interpolation
/// only), applies the configured operator, and publishes the boolean as the <c>result</c> output.
/// The engine reads that to follow the <c>true</c>/<c>false</c> outgoing edge.
/// </summary>
public sealed class ConditionNodeHandler : INodeHandler
{
    public WorkflowNodeKind Kind => WorkflowNodeKind.Condition;

    public Task<NodeRunResult> ExecuteAsync(NodeHandlerContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var node = context.Node;

        var config = NodeConfigSerializer.Deserialize<ConditionNodeConfig>(node.Config) ?? new ConditionNodeConfig();
        var left = context.Resolver.Resolve(config.Left, context.Context);
        var right = config.Right is null ? null : context.Resolver.Resolve(config.Right, context.Context);

        var outcome = Evaluate(config.Operator, left, right);
        var outputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [WorkflowPorts.ConditionResultKey] = outcome ? WorkflowPorts.True : WorkflowPorts.False,
        };
        context.Context.SetNodeOutputs(node.Name, outputs);

        return Task.FromResult(new NodeRunResult
        {
            NodeId = node.Id,
            NodeName = node.Name,
            Kind = Kind,
            Status = RunStatus.Passed,
            Outputs = outputs,
        });
    }

    private static bool Evaluate(ConditionOperator op, string left, string? right) => op switch
    {
        ConditionOperator.IsTruthy => IsTruthy(left),
        ConditionOperator.IsEmpty => string.IsNullOrEmpty(left),
        ConditionOperator.Equals => string.Equals(left, right ?? string.Empty, StringComparison.OrdinalIgnoreCase),
        ConditionOperator.NotEquals => !string.Equals(left, right ?? string.Empty, StringComparison.OrdinalIgnoreCase),
        ConditionOperator.Contains => left.Contains(right ?? string.Empty, StringComparison.OrdinalIgnoreCase),
        _ => false,
    };

    private static bool IsTruthy(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.Equals("false", StringComparison.OrdinalIgnoreCase)
        && !value.Equals("0", StringComparison.Ordinal)
        && !value.Equals("null", StringComparison.OrdinalIgnoreCase);
}
