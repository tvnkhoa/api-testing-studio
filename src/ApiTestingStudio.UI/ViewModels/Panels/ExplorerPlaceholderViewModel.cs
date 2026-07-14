namespace ApiTestingStudio.UI.ViewModels.Panels;

/// <summary>
/// Placeholder for the Service Explorer tool window (docked left). Real content — the
/// Service → Endpoint tree — arrives in Sprint 05; this exists so the docking shell has a tool pane
/// to dock, float, rearrange and persist during Sprint 04.
/// </summary>
public sealed class ExplorerPlaceholderViewModel : ToolPanelViewModel
{
    public const string PanelContentId = "tool.explorer";

    public ExplorerPlaceholderViewModel()
        : base(PanelContentId, "Explorer")
    {
    }

    /// <summary>Message shown until the Explorer is implemented.</summary>
    public string Placeholder { get; } = "The Service Explorer tree arrives in Sprint 05.";
}
