using ApiTestingStudio.Application.Workspaces;

namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Maintains the most-recently-used (MRU) list of workspaces. The list is capped and ordered
/// most-recent-first, deduplicated by location, and persisted outside any workspace database so
/// it survives app restarts. Pinning is intentionally deferred (see Sprint 02 notes).
/// </summary>
public interface IRecentWorkspacesService
{
    /// <summary>The maximum number of entries retained in the MRU list.</summary>
    static int Capacity => 10;

    /// <summary>Returns the recent workspaces, most-recent-first.</summary>
    Task<IReadOnlyList<RecentWorkspaceEntry>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts <paramref name="entry"/> at the top, or moves an existing entry with the same
    /// location to the top, trimming the list to <see cref="Capacity"/>.
    /// </summary>
    Task AddOrTouchAsync(RecentWorkspaceEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Removes the entry with the given location, if present.</summary>
    Task RemoveAsync(string location, CancellationToken cancellationToken = default);
}
