using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.Workflows;

/// <inheritdoc />
public sealed class WorkflowEngine : IWorkflowEngine
{
    /// <summary>Safety cap on nodes visited in a single linear walk, guarding against graph cycles.</summary>
    private const int MaxNodesPerWalk = 100_000;

    private readonly INodeHandlerRegistry _registry;
    private readonly IVariableResolver _resolver;
    private readonly IClock _clock;

    public WorkflowEngine(INodeHandlerRegistry registry, IVariableResolver resolver, IClock clock)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<WorkflowRunResult> RunAsync(
        Workflow workflow,
        WorkflowRunOptions? options = null,
        IWorkflowContext? context = null,
        IProgress<NodeRunResult>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        options ??= WorkflowRunOptions.Default;
        context ??= new WorkflowContext();

        var start = _clock.UtcNow;

        if (workflow.Nodes.Count == 0)
        {
            return new WorkflowRunResult
            {
                WorkflowId = workflow.Id,
                Status = RunStatus.Passed,
                Nodes = [],
                Duration = TimeSpan.Zero,
            };
        }

        try
        {
            var results = await WalkAsync(workflow, startNodeId: null, context, options, progress, cancellationToken)
                .ConfigureAwait(false);

            var failed = results.Any(r => r.Status == RunStatus.Failed);
            return new WorkflowRunResult
            {
                WorkflowId = workflow.Id,
                Status = failed ? RunStatus.Failed : RunStatus.Passed,
                Nodes = results,
                Duration = _clock.UtcNow - start,
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new WorkflowRunResult
            {
                WorkflowId = workflow.Id,
                Status = RunStatus.Cancelled,
                Nodes = [],
                Duration = _clock.UtcNow - start,
                Error = WorkflowErrors.Cancelled.Message,
            };
        }
    }

    private async Task<IReadOnlyList<NodeRunResult>> WalkAsync(
        Workflow workflow,
        Guid? startNodeId,
        IWorkflowContext context,
        WorkflowRunOptions options,
        IProgress<NodeRunResult>? progress,
        CancellationToken cancellationToken)
    {
        var results = new List<NodeRunResult>();
        var current = startNodeId is { } id ? FindNode(workflow, id) : FindEntryNode(workflow);
        var guard = 0;

        while (current is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (++guard > MaxNodesPerWalk)
            {
                break;
            }

            var result = await ExecuteNodeAsync(workflow, current, context, options, progress, cancellationToken)
                .ConfigureAwait(false);
            results.Add(result);

            if (result.Status == RunStatus.Failed && options.FailurePolicy == NodeFailurePolicy.StopOnError)
            {
                break;
            }

            current = SelectNext(workflow, current, result);
        }

        return results;
    }

    private async Task<NodeRunResult> ExecuteNodeAsync(
        Workflow workflow,
        WorkflowNode node,
        IWorkflowContext context,
        WorkflowRunOptions options,
        IProgress<NodeRunResult>? progress,
        CancellationToken cancellationToken)
    {
        progress?.Report(new NodeRunResult
        {
            NodeId = node.Id,
            NodeName = node.Name,
            Kind = node.Kind,
            Status = RunStatus.Running,
        });

        var handlerResult = _registry.Resolve(node.Kind);
        if (handlerResult.IsFailure)
        {
            return Report(progress, new NodeRunResult
            {
                NodeId = node.Id,
                NodeName = node.Name,
                Kind = node.Kind,
                Status = RunStatus.Failed,
                Error = handlerResult.Error.Message,
            });
        }

        var handlerContext = new NodeHandlerContext
        {
            Node = node,
            Workflow = workflow,
            Context = context,
            Resolver = _resolver,
            Options = options,
            RunBranch = (start, branchContext, branchToken) =>
                WalkAsync(workflow, start, branchContext, options, progress, branchToken),
        };

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (options.DefaultNodeTimeoutMs > 0 && AppliesNodeTimeout(node.Kind))
        {
            timeoutSource.CancelAfter(options.DefaultNodeTimeoutMs);
        }

        var start = _clock.UtcNow;
        try
        {
            var result = await handlerResult.Value.ExecuteAsync(handlerContext, timeoutSource.Token).ConfigureAwait(false);
            return Report(progress, result with { Duration = _clock.UtcNow - start });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Report(progress, new NodeRunResult
            {
                NodeId = node.Id,
                NodeName = node.Name,
                Kind = node.Kind,
                Status = RunStatus.Failed,
                Error = WorkflowErrors.NodeTimeout(node.Name).Message,
                Duration = _clock.UtcNow - start,
            });
        }
        catch (Exception ex)
        {
            return Report(progress, new NodeRunResult
            {
                NodeId = node.Id,
                NodeName = node.Name,
                Kind = node.Kind,
                Status = RunStatus.Failed,
                Error = WorkflowErrors.NodeFailed(node.Name, ex.Message).Message,
                Duration = _clock.UtcNow - start,
            });
        }
    }

    /// <summary>Reports a node's final result to the optional progress sink and returns it unchanged.</summary>
    private static NodeRunResult Report(IProgress<NodeRunResult>? progress, NodeRunResult result)
    {
        progress?.Report(result);
        return result;
    }

    /// <summary>Container nodes may legitimately run long; their inner nodes still get the timeout.</summary>
    private static bool AppliesNodeTimeout(WorkflowNodeKind kind) =>
        kind is not (WorkflowNodeKind.Loop or WorkflowNodeKind.Parallel or WorkflowNodeKind.Delay);

    private static WorkflowNode? SelectNext(Workflow workflow, WorkflowNode node, NodeRunResult result)
    {
        var outgoing = workflow.Edges.Where(e => e.SourceNodeId == node.Id).ToList();
        if (outgoing.Count == 0)
        {
            return null;
        }

        if (node.Kind == WorkflowNodeKind.Condition)
        {
            var branch = result.Outputs.TryGetValue(WorkflowPorts.ConditionResultKey, out var value)
                && string.Equals(value, WorkflowPorts.True, StringComparison.OrdinalIgnoreCase)
                ? WorkflowPorts.True
                : WorkflowPorts.False;

            var edge = outgoing.FirstOrDefault(e => string.Equals(e.SourcePort, branch, StringComparison.OrdinalIgnoreCase))
                ?? outgoing.FirstOrDefault(e => IsContinuationPort(e.SourcePort));
            return edge is null ? null : FindNode(workflow, edge.TargetNodeId);
        }

        var next = outgoing.FirstOrDefault(e => IsContinuationPort(e.SourcePort));
        return next is null ? null : FindNode(workflow, next.TargetNodeId);
    }

    /// <summary>A "body" edge belongs to a container node's handler and is not followed by the walk.</summary>
    private static bool IsContinuationPort(string? port) =>
        string.IsNullOrEmpty(port)
        || string.Equals(port, WorkflowPorts.Next, StringComparison.OrdinalIgnoreCase);

    private static WorkflowNode? FindNode(Workflow workflow, Guid id) =>
        workflow.Nodes.FirstOrDefault(n => n.Id == id);

    private static WorkflowNode? FindEntryNode(Workflow workflow)
    {
        if (workflow.Nodes.Count == 0)
        {
            return null;
        }

        // The entry has no incoming edge of any kind. Body-branch targets therefore never qualify
        // (they are reached only via RunBranch), and continuation targets after a container do not
        // either — the walk reaches those through SelectNext.
        var targets = workflow.Edges.Select(e => e.TargetNodeId).ToHashSet();
        return workflow.Nodes.FirstOrDefault(n => !targets.Contains(n.Id)) ?? workflow.Nodes[0];
    }
}
