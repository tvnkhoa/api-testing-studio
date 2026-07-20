using System.Collections.ObjectModel;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Backup;
using ApiTestingStudio.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ApiTestingStudio.UI.ViewModels.Dialogs;

/// <summary>
/// View model for the Backup settings + restore dialog: edits the auto-backup and retention
/// preferences (persisted to <see cref="IAppSettingsService"/>) and lists the current workspace's
/// backup archives with a per-row restore action. See ADR-0012.
/// </summary>
public sealed partial class BackupSettingsViewModel : ObservableObject
{
    private readonly IAppSettingsService _settings;
    private readonly IBackupService _backups;
    private readonly IRecoveryService _recovery;
    private readonly IWorkspaceSession _session;
    private readonly IFileDialogService _fileDialog;

    public BackupSettingsViewModel(
        IAppSettingsService settings,
        IBackupService backups,
        IRecoveryService recovery,
        IWorkspaceSession session,
        IFileDialogService fileDialog)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(backups);
        ArgumentNullException.ThrowIfNull(recovery);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(fileDialog);
        _settings = settings;
        _backups = backups;
        _recovery = recovery;
        _session = session;
        _fileDialog = fileDialog;
    }

    /// <summary>Whether to back up the workspace automatically when it is closed.</summary>
    [ObservableProperty]
    private bool _autoBackupOnClose;

    /// <summary>How many backup archives to keep per workspace before pruning the oldest.</summary>
    [ObservableProperty]
    private int _backupRetention = 10;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>Set when a restore switched the open workspace, so the shell can refresh afterwards.</summary>
    public bool WorkspaceChanged { get; private set; }

    /// <summary>Backups of the currently open workspace, newest first.</summary>
    public ObservableCollection<BackupItemViewModel> Backups { get; } = [];

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settings.LoadAsync(cancellationToken).ConfigureAwait(true);
        AutoBackupOnClose = settings.AutoBackupOnClose;
        BackupRetention = settings.BackupRetention;
        await RefreshBackupsAsync(cancellationToken).ConfigureAwait(true);
    }

    private async Task RefreshBackupsAsync(CancellationToken cancellationToken)
    {
        Backups.Clear();
        if (_session.Current is { } workspace)
        {
            var entries = await _backups.ListBackupsAsync(workspace.Id, cancellationToken).ConfigureAwait(true);
            foreach (var entry in entries)
            {
                Backups.Add(new BackupItemViewModel(entry));
            }
        }
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        var current = await _settings.LoadAsync(cancellationToken).ConfigureAwait(true);
        await _settings.SaveAsync(
            current with { AutoBackupOnClose = AutoBackupOnClose, BackupRetention = Math.Max(1, BackupRetention) },
            cancellationToken).ConfigureAwait(true);
        StatusMessage = "Backup settings saved.";
    }

    [RelayCommand]
    private async Task RestoreAsync(BackupItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        var target = _fileDialog.PromptCreateWorkspace();
        if (target is null)
        {
            return;
        }

        var result = await _recovery.RestoreAsync(item.Entry, target).ConfigureAwait(true);
        if (result.IsFailure)
        {
            StatusMessage = result.Error.Message;
            return;
        }

        WorkspaceChanged = true;
        StatusMessage = result.Value.SecretsNeedReprompt
            ? "Restored. Some secrets were created on another machine and must be re-entered."
            : "Workspace restored.";
    }
}

/// <summary>A single backup archive row for the backup settings list.</summary>
public sealed class BackupItemViewModel
{
    public BackupItemViewModel(BackupEntry entry)
    {
        Entry = entry;
    }

    public BackupEntry Entry { get; }

    public string WorkspaceName => Entry.WorkspaceName;

    public DateTimeOffset CreatedUtc => Entry.CreatedUtc;

    public string SizeDisplay => $"{Entry.SizeBytes / 1024.0:N0} KB";
}
