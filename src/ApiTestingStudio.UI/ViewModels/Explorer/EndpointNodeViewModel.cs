using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.UI.ViewModels.Explorer;

/// <summary>An endpoint (leaf) node in the Service Explorer tree.</summary>
public sealed class EndpointNodeViewModel : ExplorerNodeViewModel
{
    public EndpointNodeViewModel(
        IExplorerNodeHost host,
        Guid id,
        Guid serviceId,
        Guid? folderId,
        string name,
        HttpVerb method,
        string path,
        string? description)
        : base(host, id, name)
    {
        ServiceId = serviceId;
        FolderId = folderId;
        Method = method;
        Path = path;
        Description = description;
    }

    public Guid ServiceId { get; }

    public Guid? FolderId { get; }

    public HttpVerb Method { get; }

    public string Path { get; }

    public string? Description { get; }

    /// <summary>Short label shown in the method badge (e.g. GET, POST).</summary>
    public string MethodLabel => Method.ToString().ToUpperInvariant();
}
