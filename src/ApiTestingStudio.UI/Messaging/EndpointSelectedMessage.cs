using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.UI.Messaging;

/// <summary>
/// Broadcast on the CommunityToolkit <c>IMessenger</c> when the user selects an endpoint in the
/// Service Explorer. The API Runner (Sprint 06) subscribes to open/focus the selected endpoint.
/// Carries only display-safe identifiers so recipients re-load details from the catalog as needed.
/// </summary>
public sealed record EndpointSelectedMessage(
    Guid EndpointId,
    Guid ServiceId,
    string Name,
    HttpVerb Method,
    string Path);
