namespace ApiTestingStudio.UI.ViewModels.Explorer;

/// <summary>A service (root) node in the Service Explorer tree.</summary>
public sealed class ServiceNodeViewModel : ExplorerNodeViewModel
{
    public ServiceNodeViewModel(IExplorerNodeHost host, Guid id, string name, string? baseUrl, string? description)
        : base(host, id, name)
    {
        BaseUrl = baseUrl;
        Description = description;
    }

    public string? BaseUrl { get; }

    public string? Description { get; }
}
