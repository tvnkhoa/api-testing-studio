namespace ApiTestingStudio.UI.ViewModels.Panels;

/// <summary>
/// Base for tool-window panes (side panels such as Explorer and Logs). Tool windows are dockable,
/// floatable and auto-hideable, and are toggled from the View menu rather than closed permanently,
/// so <see cref="PanelViewModel.CanClose"/> is <c>false</c>.
/// </summary>
public abstract class ToolPanelViewModel : PanelViewModel
{
    protected ToolPanelViewModel(string contentId, string title)
        : base(contentId, title)
    {
    }

    /// <summary>Whether the tool window is currently visible in the layout.</summary>
    public bool IsVisible { get; set; } = true;

    /// <inheritdoc />
    public override bool CanClose => false;
}
