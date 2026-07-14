using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.ServiceCatalog;

/// <summary>
/// Reads the workspace's API catalog as a tree and performs the organizational CRUD for services and
/// folders (create / rename / delete / reorder). Endpoint-level operations live in
/// <see cref="IEndpointCrudService"/>. All operations require an open workspace.
/// </summary>
public interface IServiceExplorerService
{
    /// <summary>Loads the full Service → Folder → Endpoint tree for the open workspace.</summary>
    Task<Result<ServiceCatalogTree>> LoadTreeAsync(CancellationToken cancellationToken = default);

    Task<Result<Service>> CreateServiceAsync(ServiceDraft draft, CancellationToken cancellationToken = default);

    Task<Result<Service>> UpdateServiceAsync(Guid id, ServiceDraft draft, CancellationToken cancellationToken = default);

    Task<Result> DeleteServiceAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<EndpointFolder>> CreateFolderAsync(Guid serviceId, Guid? parentFolderId, string name, CancellationToken cancellationToken = default);

    Task<Result<EndpointFolder>> RenameFolderAsync(Guid id, string name, CancellationToken cancellationToken = default);

    Task<Result> DeleteFolderAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Moves a service one position up (<paramref name="up"/> true) or down among its siblings.</summary>
    Task<Result> ReorderServiceAsync(Guid id, bool up, CancellationToken cancellationToken = default);

    /// <summary>Moves a folder one position up or down among the folders that share its parent.</summary>
    Task<Result> ReorderFolderAsync(Guid id, bool up, CancellationToken cancellationToken = default);
}
