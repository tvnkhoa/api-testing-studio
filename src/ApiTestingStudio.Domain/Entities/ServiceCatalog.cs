using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Domain.Entities;

/// <summary>A logical API service (a base URL grouping related endpoints).</summary>
public sealed record Service
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkspaceId { get; init; }

    public required string Name { get; init; }

    public string? BaseUrl { get; init; }

    public string? Description { get; init; }

    /// <summary>Ordering among sibling services within the workspace (ascending).</summary>
    public int SortOrder { get; init; }
}

/// <summary>
/// A folder grouping endpoints (and sub-folders) inside a <see cref="Service"/>. Folders nest via
/// <see cref="ParentFolderId"/>: a null parent means the folder sits directly under the service,
/// otherwise it is nested inside another folder of the same service.
/// </summary>
public sealed record EndpointFolder
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid ServiceId { get; init; }

    /// <summary>Owning folder, or null when the folder sits directly under the service.</summary>
    public Guid? ParentFolderId { get; init; }

    public required string Name { get; init; }

    /// <summary>Ordering among siblings sharing the same parent (ascending).</summary>
    public int SortOrder { get; init; }
}

/// <summary>A single callable operation belonging to a <see cref="Service"/>.</summary>
public sealed record Endpoint
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid ServiceId { get; init; }

    /// <summary>Containing folder, or null when the endpoint sits directly under the service.</summary>
    public Guid? FolderId { get; init; }

    public required string Name { get; init; }

    public HttpVerb Method { get; init; } = HttpVerb.Get;

    public required string Path { get; init; }

    public string? Description { get; init; }

    /// <summary>Ordering among siblings sharing the same parent (ascending).</summary>
    public int SortOrder { get; init; }

    /// <summary>
    /// Default request headers pre-filled by the runner when this endpoint is opened, stored as a
    /// JSON array of <see cref="HttpHeader"/>. Null when none are configured.
    /// </summary>
    public string? DefaultHeaders { get; init; }

    /// <summary>Default request body text pre-filled by the runner. Null when none is configured.</summary>
    public string? DefaultBody { get; init; }
}
