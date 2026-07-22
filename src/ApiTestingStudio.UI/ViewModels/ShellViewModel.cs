using System.Collections.ObjectModel;
using System.IO;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Backup;
using ApiTestingStudio.Application.Packaging;
using ApiTestingStudio.Application.Settings;
using ApiTestingStudio.Application.Workspaces;
using ApiTestingStudio.Shared.Results;
using ApiTestingStudio.UI.Messaging;
using ApiTestingStudio.UI.Services;
using ApiTestingStudio.UI.ViewModels.Dashboard;
using ApiTestingStudio.UI.ViewModels.Dialogs;
using ApiTestingStudio.UI.ViewModels.Explorer;
using ApiTestingStudio.UI.ViewModels.Identity;
using ApiTestingStudio.UI.ViewModels.Panels;
using ApiTestingStudio.UI.ViewModels.Runner;
using ApiTestingStudio.UI.ViewModels.Stress;
using ApiTestingStudio.UI.ViewModels.Testing;
using ApiTestingStudio.UI.ViewModels.Workflow;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace ApiTestingStudio.UI.ViewModels;

/// <summary>
/// Root view model for the application shell. Owns the docking collections bound to AvalonDock and
/// every shell command (workspace lifecycle, theme, layout, panel toggles), delegating all real work
/// to application services (<see cref="IWorkspaceService"/>) and UI services
/// (<see cref="IThemeManager"/>, <see cref="IDockManager"/>). Menu and toolbar view models surface a
/// curated subset of these commands for binding.
/// </summary>
public sealed partial class ShellViewModel : ObservableObject
{
    private readonly IWorkspaceService _workspaceService;
    private readonly IWorkspaceSession _session;
    private readonly IThemeManager _themeManager;
    private readonly IDockManager _dockManager;
    private readonly IStatusBarService _statusBar;
    private readonly IFileDialogService _fileDialog;
    private readonly IDialogService _dialogService;
    private readonly IWorkspacePackageService _packageService;
    private readonly IBackupService _backupService;
    private readonly IRecoveryService _recoveryService;
    private readonly IAppSettingsService _appSettings;
    private readonly ILogger<ShellViewModel> _logger;

    private readonly ServiceExplorerViewModel _explorer;
    private readonly ApiRunnerViewModel _runner;
    private readonly WorkflowsPanelViewModel _workflows;
    private readonly ProfilesPanelViewModel _profiles;
    private readonly TestCasesPanelViewModel _testCases;
    private readonly TestResultsViewModel _testResults;
    private readonly StressRunnerViewModel _stress;
    private readonly DashboardViewModel _dashboard;
    private readonly TimelineViewModel _timeline;
    private readonly IWorkflowEditorViewModelFactory _workflowEditorFactory;
    private readonly LogViewerViewModel _logs;
    private readonly WelcomeDocumentViewModel _welcome;
    private readonly ISampleWorkspaceBuilder _sampleBuilder;

    public ShellViewModel(
        IWorkspaceService workspaceService,
        IWorkspaceSession session,
        IThemeManager themeManager,
        IDockManager dockManager,
        IStatusBarService statusBar,
        IFileDialogService fileDialog,
        IDialogService dialogService,
        IWorkspacePackageService packageService,
        IBackupService backupService,
        IRecoveryService recoveryService,
        IAppSettingsService appSettings,
        StatusBarViewModel statusBarViewModel,
        RecentWorkspacesMenuViewModel recentWorkspaces,
        ServiceExplorerViewModel explorer,
        ApiRunnerViewModel runner,
        WorkflowsPanelViewModel workflows,
        ProfilesPanelViewModel profiles,
        TestCasesPanelViewModel testCases,
        TestResultsViewModel testResults,
        StressRunnerViewModel stress,
        DashboardViewModel dashboard,
        TimelineViewModel timeline,
        LogViewerViewModel logs,
        EnvironmentSwitcherViewModel environmentSwitcher,
        ProfileSwitcherViewModel profileSwitcher,
        WelcomeDocumentViewModel welcome,
        ISampleWorkspaceBuilder sampleBuilder,
        IWorkflowEditorViewModelFactory workflowEditorFactory,
        IMessenger messenger,
        ILogger<ShellViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(workspaceService);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(themeManager);
        ArgumentNullException.ThrowIfNull(dockManager);
        ArgumentNullException.ThrowIfNull(statusBar);
        ArgumentNullException.ThrowIfNull(fileDialog);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(packageService);
        ArgumentNullException.ThrowIfNull(backupService);
        ArgumentNullException.ThrowIfNull(recoveryService);
        ArgumentNullException.ThrowIfNull(appSettings);
        ArgumentNullException.ThrowIfNull(statusBarViewModel);
        ArgumentNullException.ThrowIfNull(recentWorkspaces);
        ArgumentNullException.ThrowIfNull(explorer);
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(workflows);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(testCases);
        ArgumentNullException.ThrowIfNull(testResults);
        ArgumentNullException.ThrowIfNull(stress);
        ArgumentNullException.ThrowIfNull(dashboard);
        ArgumentNullException.ThrowIfNull(timeline);
        ArgumentNullException.ThrowIfNull(logs);
        ArgumentNullException.ThrowIfNull(environmentSwitcher);
        ArgumentNullException.ThrowIfNull(profileSwitcher);
        ArgumentNullException.ThrowIfNull(welcome);
        ArgumentNullException.ThrowIfNull(sampleBuilder);
        ArgumentNullException.ThrowIfNull(workflowEditorFactory);
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(logger);

        _workspaceService = workspaceService;
        _session = session;
        _themeManager = themeManager;
        _dockManager = dockManager;
        _statusBar = statusBar;
        _fileDialog = fileDialog;
        _dialogService = dialogService;
        _packageService = packageService;
        _backupService = backupService;
        _recoveryService = recoveryService;
        _appSettings = appSettings;
        _explorer = explorer;
        _runner = runner;
        _workflows = workflows;
        _profiles = profiles;
        _testCases = testCases;
        _testResults = testResults;
        _stress = stress;
        _dashboard = dashboard;
        _timeline = timeline;
        _logs = logs;
        _welcome = welcome;
        _sampleBuilder = sampleBuilder;
        _workflowEditorFactory = workflowEditorFactory;
        _logger = logger;

        Environments = environmentSwitcher;
        Profiles = profileSwitcher;
        StatusBar = statusBarViewModel;
        RecentWorkspaces = recentWorkspaces;
        RecentWorkspaces.OpenRequested += OnRecentOpenRequested;

        Documents.Add(_welcome);
        Tools.Add(_explorer);
        Tools.Add(_workflows);
        Tools.Add(_profiles);
        Tools.Add(_testCases);
        Tools.Add(_logs);

        // Selecting an endpoint in the Explorer opens (or focuses) the Runner document pane; the
        // runner view model itself loads the endpoint's details from the same message.
        messenger.Register<EndpointSelectedMessage>(this, (_, _) => OpenOrFocusRunner());

        // Selecting a workflow in the Workflows panel opens (or focuses) its designer document pane.
        messenger.Register<OpenWorkflowMessage>(this, (_, m) => OpenOrFocusWorkflow(m.WorkflowId, m.Name));

        // A finished test run (Sprint 11) opens (or focuses) the Test Results document with its outcomes.
        messenger.Register<ShowTestResultsMessage>(this, (_, m) => ShowTestResults(m));

        // Welcome-screen call-to-action buttons route through the shell (Sprint 16).
        messenger.Register<WelcomeActionMessage>(this, (_, m) => OnWelcomeAction(m.Action));

        Menu = new MainMenuViewModel(this, RecentWorkspaces);
        Toolbar = new ToolbarViewModel(this);
    }

    /// <summary>The window title, reflecting the open workspace.</summary>
    public string Title => _session.IsOpen
        ? $"{_session.Current?.Name} — API Testing Studio"
        : "API Testing Studio";

    /// <summary>Document panes bound to AvalonDock's <c>DocumentsSource</c>.</summary>
    public ObservableCollection<PanelViewModel> Documents { get; } = [];

    /// <summary>Tool panes bound to AvalonDock's <c>AnchorablesSource</c>.</summary>
    public ObservableCollection<PanelViewModel> Tools { get; } = [];

    public StatusBarViewModel StatusBar { get; }

    /// <summary>The active-environment switcher shown in the toolbar (Sprint 10).</summary>
    public EnvironmentSwitcherViewModel Environments { get; }

    /// <summary>The active "Run As" profile switcher shown in the toolbar (Sprint 16).</summary>
    public ProfileSwitcherViewModel Profiles { get; }

    public RecentWorkspacesMenuViewModel RecentWorkspaces { get; }

    public MainMenuViewModel Menu { get; }

    public ToolbarViewModel Toolbar { get; }

    /// <summary>Whether a workspace is currently open (drives command availability + status).</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseWorkspaceCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveWorkspaceCommand))]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportPackageCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackupNowCommand))]
    private bool _isWorkspaceOpen;

    /// <summary>Whether the dark theme is active (drives the View menu check state).</summary>
    public bool IsDarkTheme => _themeManager.Current == ThemeMode.Dark;

    /// <summary>Raised when the user chooses Exit; the window handles it by closing.</summary>
    public event EventHandler? CloseRequested;

    /// <summary>Loads initial shell state (recent list, status). Call once after the window shows.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await RecentWorkspaces.RefreshAsync(cancellationToken).ConfigureAwait(true);
        SyncWorkspaceState();
        await RefreshExplorerAsync(cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task NewWorkspaceAsync(CancellationToken cancellationToken)
    {
        var location = _fileDialog.PromptCreateWorkspace();
        if (location is null)
        {
            return;
        }

        var name = Path.GetFileNameWithoutExtension(location);
        var result = await _workspaceService.CreateAsync(location, name, description: null, cancellationToken).ConfigureAwait(true);
        await AfterWorkspaceChangeAsync(result, $"Created workspace '{name}'.", cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task OpenSampleWorkspaceAsync(CancellationToken cancellationToken)
    {
        var location = _fileDialog.PromptCreateWorkspace();
        if (location is null)
        {
            return;
        }

        var result = await _sampleBuilder.BuildAsync(location, cancellationToken).ConfigureAwait(true);
        await AfterWorkspaceChangeAsync(result, "Opened the sample workspace.", cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Routes a Welcome-screen call-to-action to the matching flow.</summary>
    private void OnWelcomeAction(WelcomeAction action)
    {
        switch (action)
        {
            case WelcomeAction.OpenSample:
                _ = OpenSampleWorkspaceCommand.ExecuteAsync(null);
                break;
            case WelcomeAction.Import when IsWorkspaceOpen:
                _ = ImportCommand.ExecuteAsync(null);
                break;
            case WelcomeAction.AddService when IsWorkspaceOpen:
                _ = _explorer.AddServiceCommand.ExecuteAsync(null);
                break;
            default:
                _statusBar.SetMessage("Open or create a workspace first — try 'Open sample workspace'.");
                break;
        }
    }

    [RelayCommand]
    private async Task OpenWorkspaceAsync(CancellationToken cancellationToken)
    {
        var location = _fileDialog.PromptOpenWorkspace();
        if (location is null)
        {
            return;
        }

        await OpenWorkspaceCoreAsync(location, cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(IsWorkspaceOpen))]
    private async Task CloseWorkspaceAsync(CancellationToken cancellationToken)
    {
        await AutoBackupBeforeCloseAsync(cancellationToken).ConfigureAwait(true);

        var result = await _workspaceService.CloseAsync(cancellationToken).ConfigureAwait(true);
        if (result.IsSuccess)
        {
            SyncWorkspaceState();
            await RecentWorkspaces.RefreshAsync(cancellationToken).ConfigureAwait(true);
            await RefreshExplorerAsync(cancellationToken).ConfigureAwait(true);
            _statusBar.SetMessage("Workspace closed.");
        }
        else
        {
            _statusBar.SetMessage(result.Error.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(IsWorkspaceOpen))]
    private Task ImportAsync() => _explorer.ImportCommand.ExecuteAsync(null);

    [RelayCommand(CanExecute = nameof(IsWorkspaceOpen))]
    private async Task ExportPackageAsync(CancellationToken cancellationToken)
    {
        var suggested = $"{_session.Current?.Name ?? "workspace"}.apistudio";
        var target = _fileDialog.PromptExportPackage(suggested);
        if (target is null)
        {
            return;
        }

        _statusBar.SetMessage("Exporting package…");
        var result = await _packageService.ExportAsync(target, cancellationToken).ConfigureAwait(true);
        if (result.IsSuccess)
        {
            _statusBar.SetMessage($"Exported package to '{target}' ({KiloBytes(result.Value.SizeBytes)}).");
        }
        else
        {
            _statusBar.SetMessage(result.Error.Message);
            _dialogService.ShowMessage("Export failed", result.Error.Message);
        }
    }

    [RelayCommand]
    private async Task ImportPackageAsync(CancellationToken cancellationToken)
    {
        var source = _fileDialog.PromptImportPackage();
        if (source is null)
        {
            return;
        }

        var target = _fileDialog.PromptCreateWorkspace();
        if (target is null)
        {
            return;
        }

        _statusBar.SetMessage("Importing package…");
        var result = await _packageService.ImportAsync(source, target, cancellationToken).ConfigureAwait(true);
        if (result.IsFailure)
        {
            _statusBar.SetMessage(result.Error.Message);
            _dialogService.ShowMessage("Import failed", result.Error.Message);
            return;
        }

        SyncWorkspaceState();
        await RecentWorkspaces.RefreshAsync(cancellationToken).ConfigureAwait(true);
        await RefreshExplorerAsync(cancellationToken).ConfigureAwait(true);

        var report = BuildImportReport(result.Value);
        _statusBar.SetMessage(report.Status);
        if (report.NeedsAttention)
        {
            _dialogService.ShowMessage("Import complete", report.Detail);
        }
    }

    [RelayCommand(CanExecute = nameof(IsWorkspaceOpen))]
    private async Task BackupNowAsync(CancellationToken cancellationToken)
    {
        _statusBar.SetMessage("Creating backup…");
        var result = await _backupService.CreateBackupAsync(cancellationToken).ConfigureAwait(true);
        if (result.IsSuccess)
        {
            _statusBar.SetMessage($"Backup created ({KiloBytes(result.Value.SizeBytes)}).");
        }
        else
        {
            _statusBar.SetMessage(result.Error.Message);
            _dialogService.ShowMessage("Backup failed", result.Error.Message);
        }
    }

    [RelayCommand]
    private async Task BackupSettingsAsync(CancellationToken cancellationToken)
    {
        var viewModel = new BackupSettingsViewModel(
            _appSettings, _backupService, _recoveryService, _session, _fileDialog);
        await viewModel.LoadAsync(cancellationToken).ConfigureAwait(true);

        _dialogService.ShowBackupSettings(viewModel);

        if (viewModel.WorkspaceChanged)
        {
            SyncWorkspaceState();
            await RecentWorkspaces.RefreshAsync(cancellationToken).ConfigureAwait(true);
            await RefreshExplorerAsync(cancellationToken).ConfigureAwait(true);
            _statusBar.SetMessage("Workspace restored from backup.");
        }
    }

    private static string KiloBytes(long bytes) => $"{bytes / 1024.0:N0} KB";

    private static (string Status, string Detail, bool NeedsAttention) BuildImportReport(PackageImportResult result)
    {
        var notes = new List<string>();
        if (result.MissingPlugins.Count > 0)
        {
            notes.Add($"Missing plugins: {string.Join(", ", result.MissingPlugins)}.");
        }

        if (result.SecretsNeedReprompt)
        {
            notes.Add("Some secrets were created on another machine and must be re-entered.");
        }

        if (notes.Count == 0)
        {
            return ("Package imported.", "Package imported.", false);
        }

        var detail = "Package imported. " + string.Join(" ", notes);
        return (detail, detail, true);
    }

    [RelayCommand(CanExecute = nameof(IsWorkspaceOpen))]
    private void SaveWorkspace()
        // Every edit is written through immediately by the SQLite storage provider — there is no
        // in-memory buffer to flush — so "Save" is an autosave confirmation, not a persist action.
        // The message states that honestly rather than implying a manual save just occurred.
        => _statusBar.SetMessage("All changes are saved automatically.");

    [RelayCommand]
    private async Task ToggleThemeAsync(CancellationToken cancellationToken)
    {
        await _themeManager.ToggleAsync(cancellationToken).ConfigureAwait(true);
        OnPropertyChanged(nameof(IsDarkTheme));
        _statusBar.SetMessage($"Theme: {_themeManager.Current}.");
    }

    [RelayCommand]
    private async Task ResetLayoutAsync(CancellationToken cancellationToken)
    {
        await _dockManager.ResetLayoutAsync(cancellationToken).ConfigureAwait(true);
        _statusBar.SetMessage("Layout reset to default.");
    }

    [RelayCommand]
    private void ToggleExplorer() => TogglePanel(_explorer);

    [RelayCommand]
    private void ToggleWorkflows() => TogglePanel(_workflows);

    [RelayCommand]
    private void ToggleProfiles() => TogglePanel(_profiles);

    [RelayCommand]
    private void ToggleTestCases() => TogglePanel(_testCases);

    [RelayCommand]
    private void ToggleLogs() => TogglePanel(_logs);

    [RelayCommand]
    private async Task OpenStressRunnerAsync(CancellationToken cancellationToken)
    {
        if (!Documents.Contains(_stress))
        {
            Documents.Add(_stress);
        }

        _stress.IsSelected = true;
        _stress.IsActive = true;
        await _stress.LoadAsync(cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task OpenDashboardAsync(CancellationToken cancellationToken)
    {
        if (!Documents.Contains(_dashboard))
        {
            Documents.Add(_dashboard);
        }

        _dashboard.IsSelected = true;
        _dashboard.IsActive = true;
        await _dashboard.LoadAsync(cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task OpenTimelineAsync(CancellationToken cancellationToken)
    {
        if (!Documents.Contains(_timeline))
        {
            Documents.Add(_timeline);
        }

        _timeline.IsSelected = true;
        _timeline.IsActive = true;
        await _timeline.LoadAsync(cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand]
    private void About()
    {
        var version = typeof(ShellViewModel).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
        _statusBar.SetMessage($"API Testing Studio v{version} — offline, workflow-first API testing by Silicon Stack.");
    }

    [RelayCommand]
    private void Exit() => CloseRequested?.Invoke(this, EventArgs.Empty);

    private async Task AutoBackupBeforeCloseAsync(CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _appSettings.LoadAsync(cancellationToken).ConfigureAwait(true);
            if (settings.AutoBackupOnClose && _session.IsOpen)
            {
                var backup = await _backupService.CreateBackupAsync(cancellationToken).ConfigureAwait(true);
                if (backup.IsFailure)
                {
                    _logger.LogWarning("Automatic backup on close failed: {Error}", backup.Error.Message);
                }
            }
        }
        catch (Exception ex)
        {
            // A failed automatic backup must never block closing the workspace.
            _logger.LogError(ex, "Automatic backup on close threw.");
        }
    }

    private async Task OpenWorkspaceCoreAsync(string location, CancellationToken cancellationToken)
    {
        var result = await _workspaceService.OpenAsync(location, cancellationToken).ConfigureAwait(true);
        await AfterWorkspaceChangeAsync(result, $"Opened workspace from '{location}'.", cancellationToken).ConfigureAwait(true);
    }

    private async Task AfterWorkspaceChangeAsync(Result<Domain.Entities.Workspace> result, string successMessage, CancellationToken cancellationToken)
    {
        if (result.IsSuccess)
        {
            SyncWorkspaceState();
            await RecentWorkspaces.RefreshAsync(cancellationToken).ConfigureAwait(true);
            await RefreshExplorerAsync(cancellationToken).ConfigureAwait(true);
            _statusBar.SetMessage(successMessage);
        }
        else
        {
            _statusBar.SetMessage(result.Error.Message);
        }
    }

    private async Task RefreshExplorerAsync(CancellationToken cancellationToken)
    {
        if (_session.IsOpen)
        {
            await _explorer.LoadAsync(cancellationToken).ConfigureAwait(true);
            await _workflows.LoadAsync(cancellationToken).ConfigureAwait(true);
            await _profiles.LoadAsync(cancellationToken).ConfigureAwait(true);
            await _testCases.LoadAsync(cancellationToken).ConfigureAwait(true);
            await _stress.LoadAsync(cancellationToken).ConfigureAwait(true);
            await _logs.LoadAsync(cancellationToken).ConfigureAwait(true);
            await Environments.LoadAsync(cancellationToken).ConfigureAwait(true);
            await Profiles.LoadAsync(cancellationToken).ConfigureAwait(true);
        }
        else
        {
            _explorer.Clear();
            _workflows.Clear();
            _profiles.Clear();
            _testCases.Clear();
            _stress.Clear();
            _dashboard.Clear();
            _timeline.Clear();
            _logs.Clear();
            Documents.Remove(_stress);
            Documents.Remove(_dashboard);
            Documents.Remove(_timeline);
            Environments.Clear();
            Profiles.Clear();
            CloseWorkflowDocuments();
        }
    }

    private void SyncWorkspaceState()
    {
        IsWorkspaceOpen = _session.IsOpen;
        _welcome.IsWorkspaceOpen = _session.IsOpen;
        StatusBar.RefreshWorkspace();
        OnPropertyChanged(nameof(Title));
    }

    private void OpenOrFocusRunner()
    {
        if (!Documents.Contains(_runner))
        {
            Documents.Add(_runner);
        }

        _runner.IsSelected = true;
        _runner.IsActive = true;
    }

    private void ShowTestResults(ShowTestResultsMessage message)
    {
        _testResults.Title = message.Title;
        _testResults.Show(message.Results);

        if (!Documents.Contains(_testResults))
        {
            Documents.Add(_testResults);
        }

        _testResults.IsSelected = true;
        _testResults.IsActive = true;
    }

    private void OpenOrFocusWorkflow(Guid workflowId, string name)
    {
        var contentId = $"document.workflow.{workflowId}";
        var pane = Documents.FirstOrDefault(d => d.ContentId == contentId);
        if (pane is null)
        {
            var editor = _workflowEditorFactory.Create(workflowId, name);
            Documents.Add(editor);
            pane = editor;
            _ = LoadWorkflowSafeAsync(editor);
        }

        pane.IsSelected = true;
        pane.IsActive = true;
    }

    private async Task LoadWorkflowSafeAsync(WorkflowEditorViewModel editor)
    {
        try
        {
            await editor.LoadAsync(CancellationToken.None).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load workflow into the designer.");
            _statusBar.SetMessage("Failed to open the workflow.");
        }
    }

    private void CloseWorkflowDocuments()
    {
        foreach (var pane in Documents.OfType<WorkflowEditorViewModel>().ToList())
        {
            Documents.Remove(pane);
        }
    }

    private void TogglePanel(ToolPanelViewModel panel)
    {
        if (Tools.Contains(panel))
        {
            Tools.Remove(panel);
        }
        else
        {
            Tools.Add(panel);
        }
    }

    private async void OnRecentOpenRequested(object? sender, string location)
    {
        try
        {
            await OpenWorkspaceCoreAsync(location, CancellationToken.None).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open recent workspace at {Location}.", location);
            _statusBar.SetMessage("Failed to open the selected workspace.");
        }
    }
}
