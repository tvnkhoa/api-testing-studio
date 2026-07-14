using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Application-level operations over a workspace lifecycle. Phase 1 defines the port; the
/// concrete use-case implementation is delivered in the Workspace Storage sprint.
/// </summary>
public interface IWorkspaceService
{
    /// <summary>Creates a new, empty workspace with the given name.</summary>
    Task<Result<Workspace>> CreateAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Loads workspace metadata by id.</summary>
    Task<Result<Workspace>> GetAsync(Guid workspaceId, CancellationToken cancellationToken = default);
}
