using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Persists <see cref="EnvironmentDefinition"/> rows for the currently open workspace.
/// EF Core types never cross this port.
/// </summary>
public interface IEnvironmentRepository
{
    /// <summary>Returns the workspace's environments, ordered by name.</summary>
    Task<IReadOnlyList<EnvironmentDefinition>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>Returns the environment with the given id, or null.</summary>
    Task<EnvironmentDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Inserts a new environment.</summary>
    Task AddAsync(EnvironmentDefinition environment, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing environment.</summary>
    Task UpdateAsync(EnvironmentDefinition environment, CancellationToken cancellationToken = default);

    /// <summary>Deletes the environment and its environment-scoped variables in one transaction.</summary>
    Task DeleteCascadeAsync(Guid environmentId, CancellationToken cancellationToken = default);
}
