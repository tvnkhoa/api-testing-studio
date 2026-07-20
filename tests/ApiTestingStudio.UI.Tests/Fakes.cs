using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Backup;
using ApiTestingStudio.Application.Packaging;
using ApiTestingStudio.Application.Settings;
using ApiTestingStudio.Application.Workspaces;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;
using ApiTestingStudio.UI.Services;

namespace ApiTestingStudio.UI.Tests;

/// <summary>Scripted <see cref="IWorkspacePackageService"/> for shell packaging commands.</summary>
internal sealed class FakeWorkspacePackageService : IWorkspacePackageService
{
    public string? LastExportTarget { get; private set; }

    public string? LastImportSource { get; private set; }

    public string? LastImportTarget { get; private set; }

    public Result<PackageExportResult>? ExportResult { get; set; }

    public Result<PackageImportResult>? ImportResult { get; set; }

    public Task<Result<PackageExportResult>> ExportAsync(string targetPackagePath, CancellationToken cancellationToken = default)
    {
        LastExportTarget = targetPackagePath;
        return Task.FromResult(ExportResult ?? Result.Success(new PackageExportResult(targetPackagePath, 2048)));
    }

    public Task<Result<PackageImportResult>> ImportAsync(string packagePath, string targetLocation, CancellationToken cancellationToken = default)
    {
        LastImportSource = packagePath;
        LastImportTarget = targetLocation;
        return Task.FromResult(ImportResult
            ?? Result.Success(new PackageImportResult(Guid.NewGuid(), targetLocation, false, [])));
    }
}

/// <summary>Scripted <see cref="IBackupService"/>.</summary>
internal sealed class FakeBackupService : IBackupService
{
    public int CreateCallCount { get; private set; }

    public Result<BackupEntry>? CreateResult { get; set; }

    public List<BackupEntry> Backups { get; } = [];

    public Task<Result<BackupEntry>> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        CreateCallCount++;
        return Task.FromResult(CreateResult
            ?? Result.Success(new BackupEntry("backup.apistudio", Guid.NewGuid(), "W", DateTimeOffset.UnixEpoch, 4096)));
    }

    public Task<IReadOnlyList<BackupEntry>> ListBackupsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BackupEntry>>(Backups);
}

/// <summary>Scripted <see cref="IRecoveryService"/>.</summary>
internal sealed class FakeRecoveryService : IRecoveryService
{
    public Task<IReadOnlyList<BackupEntry>> ListAllBackupsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BackupEntry>>([]);

    public Task<Result<PackageImportResult>> RestoreAsync(BackupEntry backup, string targetLocation, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success(new PackageImportResult(backup.WorkspaceId, targetLocation, false, [])));
}

/// <summary>In-memory <see cref="IAppSettingsService"/>.</summary>
internal sealed class FakeAppSettingsService : IAppSettingsService
{
    public AppSettings Settings { get; set; } = new();

    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Settings);

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        Settings = settings;
        return Task.CompletedTask;
    }
}

/// <summary>Mutable in-memory session the fake workspace service drives.</summary>
internal sealed class FakeWorkspaceSession : IWorkspaceSession
{
    public bool IsOpen { get; private set; }

    public Workspace? Current { get; private set; }

    public string? Location { get; private set; }

    public void Open(Workspace workspace, string location)
    {
        IsOpen = true;
        Current = workspace;
        Location = location;
    }

    public void Close()
    {
        IsOpen = false;
        Current = null;
        Location = null;
    }
}

/// <summary>Fake workspace service that records calls and mutates a <see cref="FakeWorkspaceSession"/>.</summary>
internal sealed class FakeWorkspaceService : IWorkspaceService
{
    private readonly FakeWorkspaceSession _session;

    public FakeWorkspaceService(FakeWorkspaceSession session) => _session = session;

    public bool ShouldFail { get; set; }

    public string? LastCreatedLocation { get; private set; }

    public string? LastCreatedName { get; private set; }

    public string? LastOpenedLocation { get; private set; }

    public int CloseCallCount { get; private set; }

    public Task<Result<Workspace>> CreateAsync(string location, string name, string? description = null, CancellationToken cancellationToken = default)
    {
        LastCreatedLocation = location;
        LastCreatedName = name;
        if (ShouldFail)
        {
            return Task.FromResult(Result.Failure<Workspace>(new Error("workspace.create_failed", "boom")));
        }

        var workspace = new Workspace { Name = name, Description = description };
        _session.Open(workspace, location);
        return Task.FromResult(Result.Success(workspace));
    }

    public Task<Result<Workspace>> OpenAsync(string location, CancellationToken cancellationToken = default)
    {
        LastOpenedLocation = location;
        if (ShouldFail)
        {
            return Task.FromResult(Result.Failure<Workspace>(new Error("workspace.not_found", "missing")));
        }

        var workspace = new Workspace { Name = "Opened" };
        _session.Open(workspace, location);
        return Task.FromResult(Result.Success(workspace));
    }

    public Task<Result> CloseAsync(CancellationToken cancellationToken = default)
    {
        CloseCallCount++;
        if (ShouldFail)
        {
            return Task.FromResult(Result.Failure(new Error("workspace.none_open", "none")));
        }

        _session.Close();
        return Task.FromResult(Result.Success());
    }

    public Task<Result> DeleteAsync(string location, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());
}

/// <summary>In-memory MRU store.</summary>
internal sealed class FakeRecentWorkspacesService : IRecentWorkspacesService
{
    private readonly List<RecentWorkspaceEntry> _entries = [];

    public IReadOnlyList<RecentWorkspaceEntry> Entries => _entries;

    public void Seed(params RecentWorkspaceEntry[] entries) => _entries.AddRange(entries);

    public Task<IReadOnlyList<RecentWorkspaceEntry>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<RecentWorkspaceEntry>>(_entries.ToList());

    public Task AddOrTouchAsync(RecentWorkspaceEntry entry, CancellationToken cancellationToken = default)
    {
        _entries.RemoveAll(e => e.Location == entry.Location);
        _entries.Insert(0, entry);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string location, CancellationToken cancellationToken = default)
    {
        _entries.RemoveAll(e => e.Location == location);
        return Task.CompletedTask;
    }
}

internal sealed class FakeThemeManager : IThemeManager
{
    public ThemeMode Current { get; private set; } = ThemeMode.Light;

    public int ToggleCallCount { get; private set; }

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ToggleAsync(CancellationToken cancellationToken = default)
    {
        ToggleCallCount++;
        Current = Current == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;
        return Task.CompletedTask;
    }

    public Task SetAsync(ThemeMode mode, CancellationToken cancellationToken = default)
    {
        Current = mode;
        return Task.CompletedTask;
    }
}

internal sealed class FakeDockManager : IDockManager
{
    public int ResetCallCount { get; private set; }

    public void Attach(object dockingManager)
    {
    }

    public Task<bool> LoadLayoutAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task SaveLayoutAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ResetLayoutAsync(CancellationToken cancellationToken = default)
    {
        ResetCallCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeStatusBarService : IStatusBarService
{
    public string Message { get; private set; } = string.Empty;

    public event EventHandler? MessageChanged;

    public void SetMessage(string message)
    {
        Message = message ?? string.Empty;
        MessageChanged?.Invoke(this, EventArgs.Empty);
    }
}

internal sealed class FakeFileDialogService : IFileDialogService
{
    public string? OpenResult { get; set; }

    public string? CreateResult { get; set; }

    public string? OpenFileResult { get; set; }

    public string? ExportPackageResult { get; set; }

    public string? ImportPackageResult { get; set; }

    public string? PromptOpenWorkspace() => OpenResult;

    public string? PromptCreateWorkspace() => CreateResult;

    public string? PromptOpenFile(string title, string filter) => OpenFileResult;

    public string? PromptExportPackage(string? suggestedFileName = null) => ExportPackageResult;

    public string? PromptImportPackage() => ImportPackageResult;
}
