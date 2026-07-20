using ApiTestingStudio.Application.Packaging;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Backup;

/// <summary>
/// Default <see cref="IRecoveryService"/>. Lists backups across all workspaces and restores one by
/// importing it (unpack → validate → install → open), which verifies the restored database opens
/// before reporting success. See ADR-0012.
/// </summary>
public sealed class RecoveryService : IRecoveryService
{
    private readonly IBackupStore _store;
    private readonly IWorkspacePackageService _package;
    private readonly BackupEntryReader _reader;

    public RecoveryService(IBackupStore store, IWorkspacePackageService package, BackupEntryReader reader)
    {
        _store = store;
        _package = package;
        _reader = reader;
    }

    public async Task<IReadOnlyList<BackupEntry>> ListAllBackupsAsync(CancellationToken cancellationToken = default)
    {
        var entries = new List<BackupEntry>();
        foreach (var workspaceId in _store.ListBackedUpWorkspaces())
        {
            foreach (var file in _store.ListBackupFiles(workspaceId))
            {
                var entry = await _reader.TryReadAsync(file, cancellationToken).ConfigureAwait(false);
                if (entry is not null)
                {
                    entries.Add(entry);
                }
            }
        }

        return entries.OrderByDescending(e => e.CreatedUtc).ToList();
    }

    public Task<Result<PackageImportResult>> RestoreAsync(
        BackupEntry backup,
        string targetLocation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backup);
        return _package.ImportAsync(backup.FilePath, targetLocation, cancellationToken);
    }
}
