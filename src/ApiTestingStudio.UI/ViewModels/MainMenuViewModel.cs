using CommunityToolkit.Mvvm.Input;

namespace ApiTestingStudio.UI.ViewModels;

/// <summary>
/// View model for the shell's main menu. It does not own any behaviour of its own — it exposes a
/// curated set of the <see cref="ShellViewModel"/> commands plus the recent-workspaces submenu, so
/// the menu XAML binds against a focused surface rather than the whole shell.
/// </summary>
public sealed class MainMenuViewModel
{
    private readonly ShellViewModel _shell;

    public MainMenuViewModel(ShellViewModel shell, RecentWorkspacesMenuViewModel recentWorkspaces)
    {
        ArgumentNullException.ThrowIfNull(shell);
        ArgumentNullException.ThrowIfNull(recentWorkspaces);
        _shell = shell;
        RecentWorkspaces = recentWorkspaces;
    }

    /// <summary>The shell, exposed for binding observable state such as the theme check mark.</summary>
    public ShellViewModel Shell => _shell;

    /// <summary>The File → Open Recent submenu.</summary>
    public RecentWorkspacesMenuViewModel RecentWorkspaces { get; }

    // File
    public IAsyncRelayCommand NewWorkspaceCommand => _shell.NewWorkspaceCommand;

    public IAsyncRelayCommand OpenWorkspaceCommand => _shell.OpenWorkspaceCommand;

    public IRelayCommand SaveWorkspaceCommand => _shell.SaveWorkspaceCommand;

    public IAsyncRelayCommand CloseWorkspaceCommand => _shell.CloseWorkspaceCommand;

    public IAsyncRelayCommand ImportCommand => _shell.ImportCommand;

    public IRelayCommand ExitCommand => _shell.ExitCommand;

    // View
    public IAsyncRelayCommand ToggleThemeCommand => _shell.ToggleThemeCommand;

    public IRelayCommand ToggleExplorerCommand => _shell.ToggleExplorerCommand;

    public IRelayCommand ToggleWorkflowsCommand => _shell.ToggleWorkflowsCommand;

    public IRelayCommand ToggleLogsCommand => _shell.ToggleLogsCommand;

    public IAsyncRelayCommand ResetLayoutCommand => _shell.ResetLayoutCommand;

    // Help
    public IRelayCommand AboutCommand => _shell.AboutCommand;
}
