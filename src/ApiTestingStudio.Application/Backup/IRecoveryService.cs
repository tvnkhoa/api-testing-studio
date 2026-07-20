using ApiTestingStudio.Application.Packaging;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Backup;

/// <summary>
/// Restores workspaces from backup archives (including after a crash). Restore unpacks a backup to a
/// target location and verifies the restored database opens before reporting success. See ADR-0012.
/// </summary>
public interface IRecoveryService
{
    /// <summary>Lists every backup across all workspaces, newest first.</summary>
    Task<IReadOnlyList<BackupEntry>> ListAllBackupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores <paramref name="backup"/> to <paramref name="targetLocation"/> and opens it. The
    /// returned result mirrors an import (including any secret re-prompt requirement).
    /// </summary>
    Task<Result<PackageImportResult>> RestoreAsync(
        BackupEntry backup,
        string targetLocation,
        CancellationToken cancellationToken = default);
}
