using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.UI.ViewModels.Workflow;

/// <summary>One draggable entry in the node palette.</summary>
public sealed record NodePaletteItem(WorkflowNodeKind Kind, string Label);

/// <summary>
/// Supplies the draggable node types for the designer toolbox. The palette offers only the kinds that
/// have a registered engine handler (see <see cref="INodeHandlerRegistry.SupportedKinds"/>), so a user
/// cannot drop a node the engine would fail to execute. Dropping an item creates a node of that kind.
/// </summary>
public sealed class NodePaletteViewModel
{
    public NodePaletteViewModel(IEnumerable<WorkflowNodeKind> supportedKinds)
    {
        ArgumentNullException.ThrowIfNull(supportedKinds);

        Items = supportedKinds
            .OrderBy(kind => kind)
            .Select(kind => new NodePaletteItem(kind, kind.ToString()))
            .ToList();
    }

    public IReadOnlyList<NodePaletteItem> Items { get; }
}
