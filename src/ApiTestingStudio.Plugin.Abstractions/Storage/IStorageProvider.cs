using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Plugin.Abstractions.Storage;

/// <summary>
/// Abstraction over workspace persistence. SQLite is the Phase 1 provider; the contract is
/// deliberately storage-agnostic so SQL Server / PostgreSQL / cloud providers can be added
/// later WITHOUT touching business logic. See <c>.claude/ARCHITECTURE.md</c> (Storage).
/// </summary>
public interface IStorageProvider
{
    /// <summary>Stable identifier for the backing store (e.g. "sqlite").</summary>
    string ProviderName { get; }

    /// <summary>Ensures the underlying store exists and is at the current schema version.</summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>Loads workspace metadata, or null if it does not exist.</summary>
    Task<Workspace?> GetWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>Inserts or updates workspace metadata.</summary>
    Task SaveWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken = default);
}
