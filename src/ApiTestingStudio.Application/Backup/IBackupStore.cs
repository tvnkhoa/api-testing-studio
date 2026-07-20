namespace ApiTestingStudio.Application.Backup;

/// <summary>
/// Filesystem port for the versioned backup archive store. Implemented in Infrastructure over the
/// app-data <c>backups/&lt;workspaceId&gt;/</c> folder — the only component that knows where backups
/// physically live. Keeps the backup services free of path/filesystem knowledge. See ADR-0012.
/// </summary>
public interface IBackupStore
{
    /// <summary>
    /// Returns a new, unused archive path for <paramref name="workspaceId"/> stamped with
    /// <paramref name="timestampUtc"/>, creating the workspace's backup folder if needed.
    /// </summary>
    string AllocateBackupPath(Guid workspaceId, DateTimeOffset timestampUtc);

    /// <summary>Lists existing archive file paths for a workspace, newest first.</summary>
    IReadOnlyList<string> ListBackupFiles(Guid workspaceId);

    /// <summary>Lists every workspace id that currently has at least one backup.</summary>
    IReadOnlyList<Guid> ListBackedUpWorkspaces();

    /// <summary>Deletes the oldest archives for a workspace, keeping at most <paramref name="retain"/>.</summary>
    void Prune(Guid workspaceId, int retain);
}
