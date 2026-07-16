using ApiTestingStudio.Application.Common;
using ApiTestingStudio.UI.Services;
using ApiTestingStudio.UI.ViewModels;
using ApiTestingStudio.UI.ViewModels.Explorer;
using ApiTestingStudio.UI.ViewModels.Runner;
using ApiTestingStudio.UI.ViewModels.Workflow;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTestingStudio.UI.DependencyInjection;

/// <summary>
/// Registers the UI layer: shell/panel view models and the WPF-facing services (theme, docking,
/// status bar, file dialogs) behind their interfaces. Only the composition root (Host) calls this;
/// it depends on Application ports bound by <c>AddInfrastructure</c>.
/// </summary>
public static class UiServiceCollectionExtensions
{
    public static IServiceCollection AddUi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Cross-panel messaging (Sprint 05): the Service Explorer publishes endpoint selection; the
        // API Runner (Sprint 06) subscribes. First wiring of the CommunityToolkit messenger seam.
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

        // WPF-facing services.
        services.AddSingleton<IThemeManager, ThemeManager>();
        services.AddSingleton<IDockManager, DockManagerService>();
        services.AddSingleton<IStatusBarService, StatusBarService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<IDialogService, DialogService>();

        // View models. The shell composes its menu/toolbar/panel view models internally.
        services.AddSingleton<StatusBarViewModel>();
        services.AddSingleton<RecentWorkspacesMenuViewModel>();
        services.AddSingleton<ServiceExplorerViewModel>();

        // API Runner document (Sprint 06). Child view models (builder/response/editor) are composed
        // internally; it subscribes to the injected IMessenger for endpoint selection.
        services.AddSingleton<ApiRunnerViewModel>();

        // Workflow Designer (Sprint 09). The mapper + node factory are shared singletons; the undo
        // service is per-designer (transient) and flows into each WorkflowEditorViewModel; the editor
        // is multi-instance (one pane per workflow) created via IWorkflowEditorViewModelFactory, so it
        // is NOT registered directly. The Workflows tool panel is a singleton added to the shell.
        services.AddSingleton<INodeViewModelFactory, NodeViewModelFactory>();
        services.AddSingleton<GraphMapper>();
        services.AddTransient<IUndoRedoService, UndoRedoService>();
        services.AddSingleton<IWorkflowEditorViewModelFactory, WorkflowEditorViewModelFactory>();
        services.AddSingleton<WorkflowsPanelViewModel>();

        services.AddSingleton<ShellViewModel>();

        return services;
    }
}
