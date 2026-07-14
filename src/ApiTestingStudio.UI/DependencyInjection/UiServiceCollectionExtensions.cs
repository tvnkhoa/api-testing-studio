using ApiTestingStudio.UI.Services;
using ApiTestingStudio.UI.ViewModels;
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

        // WPF-facing services.
        services.AddSingleton<IThemeManager, ThemeManager>();
        services.AddSingleton<IDockManager, DockManagerService>();
        services.AddSingleton<IStatusBarService, StatusBarService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();

        // View models. The shell composes its menu/toolbar/panel view models internally.
        services.AddSingleton<StatusBarViewModel>();
        services.AddSingleton<RecentWorkspacesMenuViewModel>();
        services.AddSingleton<ShellViewModel>();

        return services;
    }
}
