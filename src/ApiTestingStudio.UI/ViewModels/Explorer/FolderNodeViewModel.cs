namespace ApiTestingStudio.UI.ViewModels.Explorer;

/// <summary>A folder node in the Service Explorer tree.</summary>
public sealed class FolderNodeViewModel : ExplorerNodeViewModel
{
    public FolderNodeViewModel(IExplorerNodeHost host, Guid id, Guid serviceId, Guid? parentFolderId, string name)
        : base(host, id, name)
    {
        ServiceId = serviceId;
        ParentFolderId = parentFolderId;
    }

    public Guid ServiceId { get; }

    public Guid? ParentFolderId { get; }
}
