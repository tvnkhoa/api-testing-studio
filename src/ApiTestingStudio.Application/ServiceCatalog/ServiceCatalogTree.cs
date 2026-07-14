using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.ServiceCatalog;

/// <summary>
/// Immutable, hierarchical read model of a workspace's API catalog, assembled from the flat
/// repository rows so the UI binds to a ready-made tree. Children are pre-ordered by
/// <c>SortOrder</c> then name.
/// </summary>
public sealed record ServiceCatalogTree(IReadOnlyList<ServiceNode> Services)
{
    public static ServiceCatalogTree Empty { get; } = new([]);
}

/// <summary>A service and its directly-contained folders and endpoints.</summary>
public sealed record ServiceNode(
    Guid Id,
    string Name,
    string? BaseUrl,
    string? Description,
    int SortOrder,
    IReadOnlyList<FolderNode> Folders,
    IReadOnlyList<EndpointNode> Endpoints);

/// <summary>A folder, its nested sub-folders, and the endpoints it directly contains.</summary>
public sealed record FolderNode(
    Guid Id,
    Guid ServiceId,
    Guid? ParentFolderId,
    string Name,
    int SortOrder,
    IReadOnlyList<FolderNode> Folders,
    IReadOnlyList<EndpointNode> Endpoints);

/// <summary>A single endpoint leaf.</summary>
public sealed record EndpointNode(
    Guid Id,
    Guid ServiceId,
    Guid? FolderId,
    string Name,
    HttpVerb Method,
    string Path,
    string? Description,
    int SortOrder);
