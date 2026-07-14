using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Reads and writes the package/plugin dependency records stored inside the currently open
/// workspace. Operations require an open workspace. See <c>.claude/DATABASE_GUIDELINES.md</c>.
/// </summary>
public interface IPackageMetadataRepository
{
    /// <summary>Returns all recorded package dependencies for the open workspace.</summary>
    Task<IReadOnlyList<PackageMetadata>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Inserts a new record, or updates the existing one matching by <c>PluginId</c>.</summary>
    Task UpsertAsync(PackageMetadata package, CancellationToken cancellationToken = default);

    /// <summary>Removes the record for the given plugin id, if present.</summary>
    Task RemoveAsync(string pluginId, CancellationToken cancellationToken = default);
}
