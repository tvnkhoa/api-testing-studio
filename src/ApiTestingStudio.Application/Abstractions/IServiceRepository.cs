using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Persists <see cref="Service"/> rows for the currently open workspace. Operations require an open
/// workspace. EF Core types never cross this port. See <c>.claude/DATABASE_GUIDELINES.md</c>.
/// </summary>
public interface IServiceRepository
{
    /// <summary>Returns the workspace's services, ordered by <c>SortOrder</c> then name.</summary>
    Task<IReadOnlyList<Service>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>Returns the service with the given id, or null.</summary>
    Task<Service?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Inserts a new service.</summary>
    Task AddAsync(Service service, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing service (name, base url, description, ordering).</summary>
    Task UpdateAsync(Service service, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the service together with all of its folders and endpoints in a single transaction.
    /// </summary>
    Task DeleteCascadeAsync(Guid serviceId, CancellationToken cancellationToken = default);
}
