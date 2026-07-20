using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Backup;

/// <summary>
/// Creates and lists versioned backups of workspaces. A backup is a timestamped <c>.apistudio</c>
/// package written under the app-data backup store; retention prunes old archives. See ADR-0012.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Backs up the currently open workspace, pruning to the configured retention count. Fails with
    /// <c>workspace.none_open</c> when none is open.
    /// </summary>
    Task<Result<BackupEntry>> CreateBackupAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists the backups for a workspace, newest first.</summary>
    Task<IReadOnlyList<BackupEntry>> ListBackupsAsync(Guid workspaceId, CancellationToken cancellationToken = default);
}
