using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.Workflows.Handlers;

/// <summary>
/// Executes a <see cref="WorkflowNodeKind.Delay"/> node: waits the configured duration via
/// <see cref="IDelayScheduler"/> (honouring cancellation), then completes.
/// </summary>
public sealed class DelayNodeHandler : INodeHandler
{
    private readonly IDelayScheduler _scheduler;

    public DelayNodeHandler(IDelayScheduler scheduler)
    {
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
    }

    public WorkflowNodeKind Kind => WorkflowNodeKind.Delay;

    public async Task<NodeRunResult> ExecuteAsync(NodeHandlerContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var node = context.Node;

        var config = NodeConfigSerializer.Deserialize<DelayNodeConfig>(node.Config) ?? new DelayNodeConfig();
        if (config.DelayMs > 0)
        {
            await _scheduler.DelayAsync(TimeSpan.FromMilliseconds(config.DelayMs), cancellationToken).ConfigureAwait(false);
        }

        return new NodeRunResult
        {
            NodeId = node.Id,
            NodeName = node.Name,
            Kind = Kind,
            Status = RunStatus.Passed,
        };
    }
}
