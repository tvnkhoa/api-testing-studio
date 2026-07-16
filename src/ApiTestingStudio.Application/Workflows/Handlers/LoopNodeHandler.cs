using System.Globalization;
using System.Text.Json;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.Workflows.Handlers;

/// <summary>
/// Executes a <see cref="WorkflowNodeKind.Loop"/> node: iterates a resolved JSON collection or a
/// fixed count, running the body branch (the edge whose source port is <c>body</c>) once per
/// iteration and setting the item/index variables. A hard cap guards against runaway iteration.
/// </summary>
public sealed class LoopNodeHandler : INodeHandler
{
    public WorkflowNodeKind Kind => WorkflowNodeKind.Loop;

    public async Task<NodeRunResult> ExecuteAsync(NodeHandlerContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var node = context.Node;

        var config = NodeConfigSerializer.Deserialize<LoopNodeConfig>(node.Config) ?? new LoopNodeConfig();
        var max = config.MaxIterations ?? context.Options.MaxLoopIterations;

        var items = BuildIterations(config, context);
        if (items.Count > max)
        {
            return Failed(node, WorkflowErrors.LoopLimitExceeded(node.Name, max).Message);
        }

        var bodyStart = context.Workflow.Edges
            .Where(e => e.SourceNodeId == node.Id && string.Equals(e.SourcePort, WorkflowPorts.Body, StringComparison.OrdinalIgnoreCase))
            .Select(e => (Guid?)e.TargetNodeId)
            .FirstOrDefault();

        var children = new List<NodeRunResult>();
        for (var i = 0; i < items.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            context.Context.SetVariable(config.IndexVariable, i.ToString(CultureInfo.InvariantCulture));
            if (items[i] is { } item)
            {
                context.Context.SetVariable(config.ItemVariable, item);
            }

            if (bodyStart is { } start)
            {
                var iteration = await context.RunBranch(start, context.Context, cancellationToken).ConfigureAwait(false);
                children.AddRange(iteration.Select(r => r with { Iteration = i }));
            }
        }

        var outputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["count"] = items.Count.ToString(CultureInfo.InvariantCulture),
        };
        context.Context.SetNodeOutputs(node.Name, outputs);

        return new NodeRunResult
        {
            NodeId = node.Id,
            NodeName = node.Name,
            Kind = Kind,
            Status = children.Any(c => c.Status == RunStatus.Failed) ? RunStatus.Failed : RunStatus.Passed,
            Outputs = outputs,
            Children = children,
        };
    }

    private static List<string?> BuildIterations(LoopNodeConfig config, NodeHandlerContext context)
    {
        if (!string.IsNullOrWhiteSpace(config.CollectionExpression))
        {
            var resolved = context.Resolver.Resolve(config.CollectionExpression, context.Context);
            return ParseJsonArray(resolved);
        }

        if (config.Count is { } count && count > 0)
        {
            return Enumerable.Range(0, count).Select(_ => (string?)null).ToList();
        }

        return [];
    }

    private static List<string?> ParseJsonArray(string? json)
    {
        var items = new List<string?>();
        if (string.IsNullOrWhiteSpace(json))
        {
            return items;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return items;
            }

            foreach (var element in document.RootElement.EnumerateArray())
            {
                items.Add(element.ValueKind == JsonValueKind.String ? element.GetString() : element.GetRawText());
            }
        }
        catch (JsonException)
        {
            // A non-array / malformed value yields no iterations rather than throwing.
        }

        return items;
    }

    private NodeRunResult Failed(WorkflowNode node, string error) => new()
    {
        NodeId = node.Id,
        NodeName = node.Name,
        Kind = Kind,
        Status = RunStatus.Failed,
        Error = error,
    };
}
