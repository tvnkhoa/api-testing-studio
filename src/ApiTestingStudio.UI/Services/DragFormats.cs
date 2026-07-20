namespace ApiTestingStudio.UI.Services;

/// <summary>
/// Well-known <see cref="System.Windows.DataObject"/> format keys for in-app drag &amp; drop. The
/// string contract is shared by the drag source and the drop target views (e.g. the node palette /
/// Service Explorer as sources, the Workflow Designer canvas as target) so it lives in one place.
/// </summary>
public static class DragFormats
{
    /// <summary>
    /// Payload: a <see cref="ApiTestingStudio.Domain.Enums.WorkflowNodeKind"/> dragged from the
    /// Workflow Designer's node palette onto the canvas.
    /// </summary>
    public const string NodeKind = "ApiTestingStudio.NodeKind";

    /// <summary>
    /// Payload: an endpoint id (<see cref="System.Guid"/>) dragged from the Service Explorer onto
    /// the Workflow Designer canvas to create a pre-configured <c>Api</c> node.
    /// </summary>
    public const string EndpointRef = "ApiTestingStudio.EndpointRef";
}
