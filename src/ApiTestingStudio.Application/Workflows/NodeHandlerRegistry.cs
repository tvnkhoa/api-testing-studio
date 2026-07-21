using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Workflows;

/// <summary>
/// Indexes the registered <see cref="INodeHandler"/>s by kind. Built once from the DI-provided
/// handler set; a duplicate registration for the same kind is a composition error and throws.
/// </summary>
public sealed class NodeHandlerRegistry : INodeHandlerRegistry
{
    private readonly Dictionary<WorkflowNodeKind, INodeHandler> _handlers;

    public NodeHandlerRegistry(IEnumerable<INodeHandler> handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);

        var map = new Dictionary<WorkflowNodeKind, INodeHandler>();
        foreach (var handler in handlers)
        {
            if (!map.TryAdd(handler.Kind, handler))
            {
                throw new InvalidOperationException(
                    $"Duplicate workflow node handler registered for kind '{handler.Kind}'.");
            }
        }

        _handlers = map;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<WorkflowNodeKind> SupportedKinds => _handlers.Keys;

    public Result<INodeHandler> Resolve(WorkflowNodeKind kind) =>
        _handlers.TryGetValue(kind, out var handler)
            ? Result.Success(handler)
            : Result.Failure<INodeHandler>(WorkflowErrors.NoHandler(kind));
}
