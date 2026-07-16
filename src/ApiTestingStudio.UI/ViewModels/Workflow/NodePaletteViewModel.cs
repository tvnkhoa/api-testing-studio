using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.UI.ViewModels.Workflow;

/// <summary>One draggable entry in the node palette.</summary>
public sealed record NodePaletteItem(WorkflowNodeKind Kind, string Label);

/// <summary>
/// Supplies the draggable node types for the designer toolbox — one per built-in
/// <see cref="WorkflowNodeKind"/>. Dropping an item onto the canvas creates a node of that kind.
/// </summary>
public sealed class NodePaletteViewModel
{
    public IReadOnlyList<NodePaletteItem> Items { get; } =
        Enum.GetValues<WorkflowNodeKind>()
            .Select(kind => new NodePaletteItem(kind, kind.ToString()))
            .ToList();
}
