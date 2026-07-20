using System.Collections.ObjectModel;
using System.IO;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Settings;
using ApiTestingStudio.Shared.Results;
using ApiTestingStudio.UI.Messaging;
using ApiTestingStudio.UI.Services;
using ApiTestingStudio.UI.ViewModels.Dashboard;
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

    public ShellViewModel(
        IWorkspaceService workspaceService,
        IWorkspaceSession session,
        IThemeManager themeManager,
        IDockManager dockManager,
        IStatusBarService statusBar,
        IFileDialogService fileDialog,
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
        ArgumentNullException.ThrowIfNull(workflowEditorFactory);
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(logger);

        _workspaceService = workspaceService;
        _session = session;
        _themeManager = themeManager;
        _dockManager = dockManager;
        _statusBar = statusBar;
        _fileDialog = fileDialog;
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
        _workflowEditorFactory = workflowEditorFactory;
        _logger = logger;

        Environments = environmentSwitcher;
        StatusBar = statusBarViewModel;
        RecentWorkspaces = recentWorkspaces;
        RecentWorkspaces.OpenRequested += OnRecentOpenRequested;

        Documents.Add(new WelcomeDocumentViewModel());
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

    public RecentWorkspacesMenuViewModel RecentWorkspaces { get; }

    public MainMenuViewModel Menu { get; }

    public ToolbarViewModel Toolbar { get; }

    /// <summary>Whether a workspace is currently open (drives command availability + status).</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseWorkspaceCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveWorkspaceCommand))]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
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
    private void SaveWorkspace()
        // Workspace data is persisted continuously by the SQLite storage provider, so "Save" simply
        // confirms the on-disk state is current. A future sprint may flush pending editor buffers.
        => _statusBar.SetMessage("Workspace saved.");

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
    private void About() => _statusBar.SetMessage("API Testing Studio — offline workflow-first API testing. Sprint 04 shell.");

    [RelayCommand]
    private void Exit() => CloseRequested?.Invoke(this, EventArgs.Empty);

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
            CloseWorkflowDocuments();
        }
    }

    private void SyncWorkspaceState()
    {
        IsWorkspaceOpen = _session.IsOpen;
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
