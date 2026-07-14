using CommunityToolkit.Mvvm.Input;

namespace ApiTestingStudio.UI.ViewModels;

/// <summary>
/// View model for the shell toolbar. Surfaces the small set of frequently-used
/// <see cref="ShellViewModel"/> commands shown as toolbar buttons.
/// </summary>
public sealed class ToolbarViewModel
{
    private readonly ShellViewModel _shell;

    public ToolbarViewModel(ShellViewModel shell)
    {
        ArgumentNullException.ThrowIfNull(shell);
        _shell = shell;
    }

    /// <summary>The shell, exposed for binding observable state (e.g. the theme toggle).</summary>
    public ShellViewModel Shell => _shell;

    public IAsyncRelayCommand NewWorkspaceCommand => _shell.NewWorkspaceCommand;

    public IAsyncRelayCommand OpenWorkspaceCommand => _shell.OpenWorkspaceCommand;

    public IRelayCommand SaveWorkspaceCommand => _shell.SaveWorkspaceCommand;

    public IAsyncRelayCommand ToggleThemeCommand => _shell.ToggleThemeCommand;
}
