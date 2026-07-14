namespace ApiTestingStudio.UI.ViewModels.Panels;

/// <summary>
/// Base for document panes — the primary editing surfaces shown in the centre of the shell
/// (Runner, Workflow Designer, Dashboard in later sprints). Documents are tabbed, reorderable and
/// closeable.
/// </summary>
public abstract class DocumentPanelViewModel : PanelViewModel
{
    protected DocumentPanelViewModel(string contentId, string title)
        : base(contentId, title)
    {
    }
}
