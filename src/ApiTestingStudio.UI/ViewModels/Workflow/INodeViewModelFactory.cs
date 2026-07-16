using System.Windows;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.UI.ViewModels.Workflow;

/// <summary>
/// Builds <see cref="NodeViewModel"/>s — either fresh for a palette drop (default ports + default
/// typed config) or from a persisted domain <see cref="WorkflowNode"/>. Ports come from the
/// Application <c>NodePortCatalog</c> so the designer and engine agree on the port vocabulary.
/// </summary>
public interface INodeViewModelFactory
{
    /// <summary>Creates a new node of <paramref name="kind"/> at <paramref name="location"/>.</summary>
    NodeViewModel Create(WorkflowNodeKind kind, Point location);

    /// <summary>Rebuilds a node view model from a persisted domain node (config, position, size, colour).</summary>
    NodeViewModel FromDomain(WorkflowNode node);
}
