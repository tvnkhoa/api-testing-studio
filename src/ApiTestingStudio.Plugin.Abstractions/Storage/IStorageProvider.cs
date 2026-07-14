using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Plugin.Abstractions.Storage;

/// <summary>
/// Abstraction over workspace persistence and its lifecycle. SQLite is the Phase 1 provider; the
/// contract is deliberately storage-agnostic so SQL Server / PostgreSQL / cloud providers can be
/// added later WITHOUT touching business logic. See <c>.claude/ARCHITECTURE.md</c> (Storage).
///
/// <para>A <c>location</c> is an opaque, provider-specific locator. The SQLite provider treats it
/// as a file path; a server provider might treat it as a connection string or database name.
/// Exactly one workspace is open at a time; lifecycle methods mutate that open state.</para>
/// </summary>
public interface IStorageProvider
{
    /// <summary>Stable identifier for the backing store (e.g. "sqlite").</summary>
    string ProviderName { get; }

    /// <summary>Whether a workspace is currently open on this provider.</summary>
    bool IsOpen { get; }

    /// <summary>
    /// Creates a new, empty workspace at <paramref name="location"/>, provisions its schema, and
    /// leaves it open. Fails if a workspace already exists at the location.
    /// </summary>
    Task<Result> CreateAsync(string location, Workspace metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the existing workspace at <paramref name="location"/>, applying pending migrations,
    /// and leaves it open. Fails (without throwing) when the target is missing, locked, corrupt,
    /// or written by a newer schema than this build supports.
    /// </summary>
    Task<Result> OpenAsync(string location, CancellationToken cancellationToken = default);

    /// <summary>Closes the currently open workspace, releasing store handles. No-op when none is open.</summary>
    Task CloseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes the workspace at <paramref name="location"/>. If it is currently open
    /// it is closed first. Fails (without throwing) when nothing exists at the location.
    /// </summary>
    Task<Result> DeleteAsync(string location, CancellationToken cancellationToken = default);

    /// <summary>Loads metadata for the currently open workspace, or null when none is open.</summary>
    Task<Workspace?> GetWorkspaceAsync(CancellationToken cancellationToken = default);

    /// <summary>Updates metadata for the currently open workspace.</summary>
    Task SaveWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken = default);
}
