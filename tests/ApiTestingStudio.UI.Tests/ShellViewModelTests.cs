using ApiTestingStudio.Application.Settings;
using ApiTestingStudio.Application.Testing;
using ApiTestingStudio.Core.Plugins;
using ApiTestingStudio.Plugin.Abstractions.Assertions;
using ApiTestingStudio.UI.ViewModels;
using ApiTestingStudio.UI.ViewModels.Explorer;
using ApiTestingStudio.UI.ViewModels.Identity;
using ApiTestingStudio.UI.ViewModels.Runner;
using ApiTestingStudio.UI.ViewModels.Stress;
using ApiTestingStudio.UI.ViewModels.Testing;
using ApiTestingStudio.UI.ViewModels.Workflow;
using CommunityToolkit.Mvvm.Messaging;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiTestingStudio.UI.Tests;

public sealed class ShellViewModelTests
{
    private static ShellHarness CreateShell()
    {
        var session = new FakeWorkspaceSession();
        var workspaceService = new FakeWorkspaceService(session);
        var recent = new FakeRecentWorkspacesService();
        var theme = new FakeThemeManager();
        var dock = new FakeDockManager();
        var status = new FakeStatusBarService();
        var dialog = new FakeFileDialogService();

        var statusVm = new StatusBarViewModel(session, status);
        var recentVm = new RecentWorkspacesMenuViewModel(recent);
        var explorer = new ServiceExplorerViewModel(
            new FakeServiceExplorerService(),
            new FakeEndpointCrudService(),
            new FakeExplorerStateService(),
            new WeakReferenceMessenger(),
            new FakeDialogService(),
            session,
            status,
            NullLogger<ServiceExplorerViewModel>.Instance);
        var messenger = new WeakReferenceMessenger();
        var runner = new ApiRunnerViewModel(
            new FakeRequestExecutionService(),
            new FakeRequestHistoryService(),
            new FakeEndpointRepository(),
            new FakeServiceRepository(),
            messenger,
            status,
            NullLogger<ApiRunnerViewModel>.Instance);
        var workflows = new WorkflowsPanelViewModel(
            new FakeWorkflowCatalogService(),
            messenger,
            new FakeDialogService(),
            session,
            status,
            NullLogger<WorkflowsPanelViewModel>.Instance);
        var profiles = new ProfilesPanelViewModel(
            new FakeProfileService(),
            new FakeEnvironmentService(),
            new FakeVariableService(),
            new FakeDialogService(),
            status,
            session,
            messenger);
        var environmentSwitcher = new EnvironmentSwitcherViewModel(
            new FakeEnvironmentService(),
            session,
            messenger);
        var testCases = new TestCasesPanelViewModel(
            new FakeTestSuiteRepository(),
            new FakeTestCaseRepository(),
            new FakeTestSuiteExecutor(),
            new FakeServiceRepository(),
            new FakeEndpointRepository(),
            new FakeShellWorkflowRepository(),
            Array.Empty<IAssertion>(),
            new FakeDialogService(),
            status,
            session,
            messenger);
        var testResults = new TestResultsViewModel(new TestReportBuilder());
        var stress = new StressRunnerViewModel(
            new FakeStressOrchestrator(),
            new FakeServiceRepository(),
            new FakeEndpointRepository(),
            new FakeShellWorkflowRepository(),
            session,
            status,
            new PluginRegistry([]),
            NullLogger<StressRunnerViewModel>.Instance);
        var vm = new ShellViewModel(
            workspaceService, session, theme, dock, status, dialog, statusVm, recentVm, explorer,
            runner, workflows, profiles, testCases, testResults, stress, environmentSwitcher,
            new FakeWorkflowEditorViewModelFactory(), messenger,
            NullLogger<ShellViewModel>.Instance);

        return new ShellHarness(vm, workspaceService, session, recent, theme, dock, status, dialog);
    }

    [Fact]
    public async Task New_workspace_when_dialog_cancelled_does_nothing()
    {
        var h = CreateShell();
        h.Dialog.CreateResult = null;

        await h.ViewModel.NewWorkspaceCommand.ExecuteAsync(null);

        h.WorkspaceService.LastCreatedLocation.Should().BeNull();
        h.ViewModel.IsWorkspaceOpen.Should().BeFalse();
    }

    [Fact]
    public async Task New_workspace_creates_with_name_derived_from_the_file_and_opens()
    {
        var h = CreateShell();
        h.Dialog.CreateResult = @"C:\temp\MyApi.atsdb";

        await h.ViewModel.NewWorkspaceCommand.ExecuteAsync(null);

        h.WorkspaceService.LastCreatedLocation.Should().Be(@"C:\temp\MyApi.atsdb");
        h.WorkspaceService.LastCreatedName.Should().Be("MyApi");
        h.ViewModel.IsWorkspaceOpen.Should().BeTrue();
        h.ViewModel.StatusBar.WorkspaceName.Should().Be("MyApi");
    }

    [Fact]
    public async Task Open_workspace_success_opens_and_refreshes_recent_menu()
    {
        var h = CreateShell();
        h.Dialog.OpenResult = @"C:\temp\Existing.atsdb";
        h.Recent.Seed(new Application.Workspaces.RecentWorkspaceEntry
        {
            Location = @"C:\temp\Existing.atsdb",
            Name = "Existing",
            LastOpenedUtc = DateTimeOffset.UnixEpoch,
        });

        await h.ViewModel.OpenWorkspaceCommand.ExecuteAsync(null);

        h.WorkspaceService.LastOpenedLocation.Should().Be(@"C:\temp\Existing.atsdb");
        h.ViewModel.IsWorkspaceOpen.Should().BeTrue();
        h.ViewModel.RecentWorkspaces.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Open_workspace_failure_reports_the_error_and_stays_closed()
    {
        var h = CreateShell();
        h.Dialog.OpenResult = @"C:\temp\Missing.atsdb";
        h.WorkspaceService.ShouldFail = true;

        await h.ViewModel.OpenWorkspaceCommand.ExecuteAsync(null);

        h.ViewModel.IsWorkspaceOpen.Should().BeFalse();
        h.Status.Message.Should().Be("missing");
    }

    [Fact]
    public async Task Close_workspace_is_disabled_until_a_workspace_is_open()
    {
        var h = CreateShell();
        h.ViewModel.CloseWorkspaceCommand.CanExecute(null).Should().BeFalse();

        h.Dialog.CreateResult = @"C:\temp\W.atsdb";
        await h.ViewModel.NewWorkspaceCommand.ExecuteAsync(null);

        h.ViewModel.CloseWorkspaceCommand.CanExecute(null).Should().BeTrue();

        await h.ViewModel.CloseWorkspaceCommand.ExecuteAsync(null);

        h.ViewModel.IsWorkspaceOpen.Should().BeFalse();
        h.WorkspaceService.CloseCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Toggle_theme_delegates_to_the_theme_manager_and_updates_state()
    {
        var h = CreateShell();
        h.ViewModel.IsDarkTheme.Should().BeFalse();

        await h.ViewModel.ToggleThemeCommand.ExecuteAsync(null);

        h.Theme.ToggleCallCount.Should().Be(1);
        h.Theme.Current.Should().Be(ThemeMode.Dark);
        h.ViewModel.IsDarkTheme.Should().BeTrue();
    }

    [Fact]
    public async Task Reset_layout_delegates_to_the_dock_manager()
    {
        var h = CreateShell();

        await h.ViewModel.ResetLayoutCommand.ExecuteAsync(null);

        h.Dock.ResetCallCount.Should().Be(1);
    }

    [Fact]
    public void Toggle_explorer_removes_then_restores_the_tool_pane()
    {
        var h = CreateShell();
        h.ViewModel.Tools.Should().HaveCount(5);

        h.ViewModel.ToggleExplorerCommand.Execute(null);
        h.ViewModel.Tools.Should().NotContain(p => p.ContentId == "tool.explorer");

        h.ViewModel.ToggleExplorerCommand.Execute(null);
        h.ViewModel.Tools.Should().Contain(p => p.ContentId == "tool.explorer");
    }

    private sealed record ShellHarness(
        ShellViewModel ViewModel,
        FakeWorkspaceService WorkspaceService,
        FakeWorkspaceSession Session,
        FakeRecentWorkspacesService Recent,
        FakeThemeManager Theme,
        FakeDockManager Dock,
        FakeStatusBarService Status,
        FakeFileDialogService Dialog);
}
