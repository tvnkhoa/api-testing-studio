using System.Globalization;
using ApiTestingStudio.Application.Testing;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.Workflows.Handlers;

/// <summary>
/// Executes a <see cref="WorkflowNodeKind.Assertion"/> node: reads the response outputs
/// (<c>status</c>/<c>reason</c>/<c>body</c>) published by an upstream node and evaluates the
/// configured assertions through the shared <see cref="IAssertionRunner"/> — the same runner a test
/// case uses, so the assertion context is identical in both places. The node passes only when every
/// assertion passes.
/// </summary>
public sealed class AssertionNodeHandler : INodeHandler
{
    private readonly IAssertionRunner _assertionRunner;

    public AssertionNodeHandler(IAssertionRunner assertionRunner)
    {
        _assertionRunner = assertionRunner ?? throw new ArgumentNullException(nameof(assertionRunner));
    }

    public WorkflowNodeKind Kind => WorkflowNodeKind.Assertion;

    public async Task<NodeRunResult> ExecuteAsync(NodeHandlerContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var node = context.Node;

        var config = NodeConfigSerializer.Deserialize<AssertionNodeConfig>(node.Config);
        if (config is null || config.Assertions.Count == 0)
        {
            return Failed(node, WorkflowErrors.InvalidConfig(node.Name, "at least one assertion is required.").Message);
        }

        if (string.IsNullOrWhiteSpace(config.SourceNode)
            || !context.Context.TryGetNodeOutputs(config.SourceNode, out var outputs))
        {
            return Failed(node, WorkflowErrors.InvalidConfig(node.Name, $"source node '{config.SourceNode}' has no response outputs.").Message);
        }

        var execution = SyntheticHttpResponse.FromNodeOutputs(outputs, TimeSpan.Zero);
        var definitions = config.Assertions.Select(ToDefinition).ToList();

        var results = await _assertionRunner.EvaluateAsync(execution, definitions, cancellationToken).ConfigureAwait(false);

        var passed = results.Count(r => r.Outcome == AssertionOutcome.Passed);
        var failed = results.Count(r => r.Outcome == AssertionOutcome.Failed);
        var skipped = results.Count(r => r.Outcome == AssertionOutcome.Skipped);

        var nodeOutputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["passed"] = passed.ToString(CultureInfo.InvariantCulture),
            ["failed"] = failed.ToString(CultureInfo.InvariantCulture),
            ["skipped"] = skipped.ToString(CultureInfo.InvariantCulture),
        };
        context.Context.SetNodeOutputs(node.Name, nodeOutputs);

        if (failed > 0)
        {
            var reasons = string.Join("; ", results.Where(r => r.Outcome == AssertionOutcome.Failed).Select(r => r.Message));
            return new NodeRunResult
            {
                NodeId = node.Id,
                NodeName = node.Name,
                Kind = Kind,
                Status = RunStatus.Failed,
                Outputs = nodeOutputs,
                Error = $"{failed} assertion(s) failed: {reasons}",
            };
        }

        return new NodeRunResult
        {
            NodeId = node.Id,
            NodeName = node.Name,
            Kind = Kind,
            Status = RunStatus.Passed,
            Outputs = nodeOutputs,
        };
    }

    private static AssertionDefinition ToDefinition(AssertionSpec spec) => new()
    {
        Kind = spec.Kind,
        Source = spec.Source,
        Target = spec.Target,
        Expression = spec.Expression,
        Operator = spec.Operator,
        Expected = spec.Expected,
        Enabled = spec.Enabled,
    };

    private NodeRunResult Failed(WorkflowNode node, string error) => new()
    {
        NodeId = node.Id,
        NodeName = node.Name,
        Kind = Kind,
        Status = RunStatus.Failed,
        Error = error,
    };
}
