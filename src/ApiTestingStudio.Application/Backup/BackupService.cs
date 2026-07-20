using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Packaging;
using ApiTestingStudio.Application.Workspaces;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Backup;

/// <summary>
/// Default <see cref="IBackupService"/>. A backup is a timestamped <c>.apistudio</c> package written
/// into the app-data backup store; creation reuses the export orchestrator and then prunes old
/// archives to the configured retention. See ADR-0012.
/// </summary>
public sealed class BackupService : IBackupService
{
    private readonly IWorkspaceSession _session;
    private readonly IWorkspacePackageService _package;
    private readonly IBackupStore _store;
    private readonly IAppSettingsService _settings;
    private readonly IClock _clock;
    private readonly BackupEntryReader _reader;

    public BackupService(
        IWorkspaceSession session,
        IWorkspacePackageService package,
        IBackupStore store,
        IAppSettingsService settings,
        IClock clock,
        BackupEntryReader reader)
    {
        _session = session;
        _package = package;
        _store = store;
        _settings = settings;
        _clock = clock;
        _reader = reader;
    }

    public async Task<Result<BackupEntry>> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        if (_session is not { IsOpen: true, Current: { } workspace })
        {
            return Result.Failure<BackupEntry>(WorkspaceErrors.NoneOpen);
        }

        var timestamp = _clock.UtcNow;
        var path = _store.AllocateBackupPath(workspace.Id, timestamp);

        var export = await _package.ExportAsync(path, cancellationToken).ConfigureAwait(false);
        if (export.IsFailure)
        {
            return Result.Failure<BackupEntry>(export.Error);
        }

        var settings = await _settings.LoadAsync(cancellationToken).ConfigureAwait(false);
        _store.Prune(workspace.Id, Math.Max(1, settings.BackupRetention));

        return Result.Success(new BackupEntry(
            path,
            workspace.Id,
            workspace.Name,
            timestamp,
            export.Value.SizeBytes));
    }

    public async Task<IReadOnlyList<BackupEntry>> ListBackupsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var entries = new List<BackupEntry>();
        foreach (var file in _store.ListBackupFiles(workspaceId))
        {
            var entry = await _reader.TryReadAsync(file, cancellationToken).ConfigureAwait(false);
            if (entry is not null)
            {
                entries.Add(entry);
            }
        }

        return entries.OrderByDescending(e => e.CreatedUtc).ToList();
    }
}
