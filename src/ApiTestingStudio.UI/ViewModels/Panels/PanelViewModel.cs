using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTestingStudio.UI.ViewModels.Panels;

/// <summary>
/// Base view model for every dockable pane in the shell (documents and tool windows). AvalonDock
/// binds each pane to one of these through <c>DocumentsSource</c>/<c>AnchorablesSource</c>, and
/// re-associates saved layout entries with live view models by matching <see cref="ContentId"/>.
/// This is the panel-registration seam later sprints build on (feature panels and plugin
/// <c>IToolWindow</c>s derive their view models from <see cref="ToolPanelViewModel"/> /
/// <see cref="DocumentPanelViewModel"/>).
/// </summary>
public abstract partial class PanelViewModel : ObservableObject
{
    protected PanelViewModel(string contentId, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ContentId = contentId;
        _title = title;
    }

    /// <summary>Stable identifier used to persist and restore this pane across sessions.</summary>
    public string ContentId { get; }

    /// <summary>The pane title shown on its tab/header.</summary>
    [ObservableProperty]
    private string _title;

    /// <summary>Whether this pane is the active (focused) one in the docking manager.</summary>
    [ObservableProperty]
    private bool _isActive;

    /// <summary>Whether this pane is the selected tab within its pane group.</summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>Whether the user may close this pane. Tool windows hide instead of closing.</summary>
    public virtual bool CanClose => true;
}
