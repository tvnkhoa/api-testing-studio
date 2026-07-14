using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Persists <see cref="EndpointFolder"/> rows for the currently open workspace. Operations require an
/// open workspace. EF Core types never cross this port. See <c>.claude/DATABASE_GUIDELINES.md</c>.
/// </summary>
public interface IEndpointFolderRepository
{
    /// <summary>Returns all folders belonging to a service, ordered by <c>SortOrder</c> then name.</summary>
    Task<IReadOnlyList<EndpointFolder>> GetByServiceAsync(Guid serviceId, CancellationToken cancellationToken = default);

    /// <summary>Returns the folder with the given id, or null.</summary>
    Task<EndpointFolder?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Inserts a new folder.</summary>
    Task AddAsync(EndpointFolder folder, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing folder (name, parent, ordering).</summary>
    Task UpdateAsync(EndpointFolder folder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the folder, all of its descendant folders, and every endpoint they contain in a
    /// single transaction.
    /// </summary>
    Task DeleteCascadeAsync(Guid folderId, CancellationToken cancellationToken = default);
}
