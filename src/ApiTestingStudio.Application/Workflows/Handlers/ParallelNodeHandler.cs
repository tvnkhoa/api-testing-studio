using System.Globalization;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.Workflows.Handlers;

/// <summary>
/// Executes a <see cref="WorkflowNodeKind.Parallel"/> node: fans out every body branch (edges whose
/// source port is <c>body</c>) concurrently, bounded by a configurable degree of parallelism, then
/// fans the results back in. Branches write outputs under their own node names, so the shared
/// context stays collision-free.
/// </summary>
public sealed class ParallelNodeHandler : INodeHandler
{
    private const string BodyPort = "body";

    public WorkflowNodeKind Kind => WorkflowNodeKind.Parallel;

    public async Task<NodeRunResult> ExecuteAsync(NodeHandlerContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var node = context.Node;

        var config = NodeConfigSerializer.Deserialize<ParallelNodeConfig>(node.Config) ?? new ParallelNodeConfig();
        var degree = config.MaxDegreeOfParallelism ?? context.Options.DefaultMaxDegreeOfParallelism;
        if (degree < 1)
        {
            degree = 1;
        }

        var branchStarts = context.Workflow.Edges
            .Where(e => e.SourceNodeId == node.Id && string.Equals(e.SourcePort, BodyPort, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.TargetNodeId)
            .ToList();

        using var semaphore = new SemaphoreSlim(degree);
        var tasks = branchStarts.Select(async start =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await context.RunBranch(start, context.Context, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        }).ToList();

        var branchResults = await Task.WhenAll(tasks).ConfigureAwait(false);

        var children = new List<NodeRunResult>();
        for (var i = 0; i < branchResults.Length; i++)
        {
            children.AddRange(branchResults[i].Select(r => r with { Iteration = i }));
        }

        var outputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["branches"] = branchStarts.Count.ToString(CultureInfo.InvariantCulture),
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
}
