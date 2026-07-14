using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Persists <see cref="Endpoint"/> rows for the currently open workspace. Operations require an open
/// workspace. EF Core types never cross this port. See <c>.claude/DATABASE_GUIDELINES.md</c>.
/// </summary>
public interface IEndpointRepository
{
    /// <summary>Returns all endpoints of a service, ordered by <c>SortOrder</c> then name.</summary>
    Task<IReadOnlyList<Endpoint>> GetByServiceAsync(Guid serviceId, CancellationToken cancellationToken = default);

    /// <summary>Returns the endpoint with the given id, or null.</summary>
    Task<Endpoint?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Inserts a new endpoint.</summary>
    Task AddAsync(Endpoint endpoint, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing endpoint (name, method, path, description, parent, ordering).</summary>
    Task UpdateAsync(Endpoint endpoint, CancellationToken cancellationToken = default);

    /// <summary>Deletes the endpoint with the given id, if present.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
