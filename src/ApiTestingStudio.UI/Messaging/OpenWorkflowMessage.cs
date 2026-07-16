namespace ApiTestingStudio.UI.Messaging;

/// <summary>
/// Broadcast on the CommunityToolkit <c>IMessenger</c> when the user selects a workflow in the
/// Workflows tool panel. The shell subscribes to open (or focus) its designer document pane. Mirrors
/// <see cref="EndpointSelectedMessage"/>; carries only display-safe identifiers.
/// </summary>
public sealed record OpenWorkflowMessage(Guid WorkflowId, string Name);
