namespace ApiTestingStudio.UI.ViewModels.Panels;

/// <summary>
/// Placeholder for the Logs tool window (docked bottom). The execution/run log tree arrives in
/// Sprint 13; this exists so Sprint 04 has a second tool pane to exercise docking and layout
/// persistence.
/// </summary>
public sealed class LogsPlaceholderViewModel : ToolPanelViewModel
{
    public const string PanelContentId = "tool.logs";

    public LogsPlaceholderViewModel()
        : base(PanelContentId, "Logs")
    {
    }

    /// <summary>Message shown until logging is implemented.</summary>
    public string Placeholder { get; } = "Execution logs arrive in Sprint 13.";
}
