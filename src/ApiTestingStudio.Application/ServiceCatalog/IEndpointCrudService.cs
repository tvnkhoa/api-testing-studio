using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.ServiceCatalog;

/// <summary>
/// Endpoint-level operations for the Service Explorer: create, edit, duplicate, delete, move between
/// folders, and reorder among siblings. All operations require an open workspace.
/// </summary>
public interface IEndpointCrudService
{
    Task<Result<Endpoint>> CreateEndpointAsync(Guid serviceId, Guid? folderId, EndpointDraft draft, CancellationToken cancellationToken = default);

    Task<Result<Endpoint>> UpdateEndpointAsync(Guid id, EndpointDraft draft, CancellationToken cancellationToken = default);

    /// <summary>Creates a copy of an endpoint (suffixed name) in the same folder.</summary>
    Task<Result<Endpoint>> DuplicateEndpointAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result> DeleteEndpointAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Moves an endpoint to another folder within the same service (null = service root).</summary>
    Task<Result> MoveEndpointAsync(Guid id, Guid? targetFolderId, CancellationToken cancellationToken = default);

    /// <summary>Moves an endpoint one position up or down among the endpoints that share its parent.</summary>
    Task<Result> ReorderEndpointAsync(Guid id, bool up, CancellationToken cancellationToken = default);
}
