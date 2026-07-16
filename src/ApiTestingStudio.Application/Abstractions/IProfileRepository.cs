using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Persists <see cref="ProfileDefinition"/> rows for the currently open workspace. Secret-bearing
/// fields are stored as ciphertext only. EF Core types never cross this port.
/// </summary>
public interface IProfileRepository
{
    /// <summary>Returns the workspace's profiles, ordered by name.</summary>
    Task<IReadOnlyList<ProfileDefinition>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>Returns the profile with the given id, or null.</summary>
    Task<ProfileDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Inserts a new profile.</summary>
    Task AddAsync(ProfileDefinition profile, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing profile.</summary>
    Task UpdateAsync(ProfileDefinition profile, CancellationToken cancellationToken = default);

    /// <summary>Deletes the profile with the given id.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
