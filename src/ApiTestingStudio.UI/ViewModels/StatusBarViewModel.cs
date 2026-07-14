using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTestingStudio.UI.ViewModels;

/// <summary>
/// Drives the shell status bar: the open workspace's name, the connection indicator (the app is
/// offline-only, so this is always "Offline"), and the latest background-task message from
/// <see cref="IStatusBarService"/>. The workspace name is refreshed by the shell after workspace
/// lifecycle operations via <see cref="RefreshWorkspace"/>.
/// </summary>
public sealed partial class StatusBarViewModel : ObservableObject
{
    private const string NoWorkspaceText = "No workspace open";

    private readonly IWorkspaceSession _session;
    private readonly IStatusBarService _statusBar;

    public StatusBarViewModel(IWorkspaceSession session, IStatusBarService statusBar)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(statusBar);
        _session = session;
        _statusBar = statusBar;

        _workspaceName = NoWorkspaceText;
        _backgroundStatus = statusBar.Message;
        _statusBar.MessageChanged += OnMessageChanged;
    }

    /// <summary>Name of the open workspace, or a placeholder when none is open.</summary>
    [ObservableProperty]
    private string _workspaceName;

    /// <summary>Latest background-task message (empty when idle).</summary>
    [ObservableProperty]
    private string _backgroundStatus;

    /// <summary>Connection indicator. The application is offline-only, so this is constant.</summary>
    public string ConnectionStatus { get; } = "Offline";

    /// <summary>Re-reads the open workspace from the session. Called by the shell after open/close.</summary>
    public void RefreshWorkspace()
        => WorkspaceName = _session.IsOpen ? _session.Current?.Name ?? NoWorkspaceText : NoWorkspaceText;

    private void OnMessageChanged(object? sender, EventArgs e) => BackgroundStatus = _statusBar.Message;
}
