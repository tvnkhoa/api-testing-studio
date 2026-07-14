namespace ApiTestingStudio.UI.ViewModels.Panels;

/// <summary>
/// The default document shown in the centre pane when the shell opens. Gives the docking manager a
/// document pane to host and persist; feature documents (Runner, Workflow, Dashboard) replace it in
/// later sprints.
/// </summary>
public sealed class WelcomeDocumentViewModel : DocumentPanelViewModel
{
    public const string PanelContentId = "document.welcome";

    public WelcomeDocumentViewModel()
        : base(PanelContentId, "Welcome")
    {
    }

    /// <summary>Headline shown on the welcome document.</summary>
    public string Headline { get; } = "API Testing Studio";

    /// <summary>Sub-text shown on the welcome document.</summary>
    public string Message { get; } =
        "Create or open a workspace from the File menu to get started. " +
        "Feature panels dock into this shell in the sprints ahead.";
}
