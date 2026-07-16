using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTestingStudio.UI.ViewModels.Workflow;

/// <summary>
/// A directed edge between an output port and an input port — the data context of a Nodify
/// <c>Connection</c>. The connection line binds to <c>Source.Anchor</c>/<c>Target.Anchor</c>.
/// <see cref="Mapping"/> carries the optional data-mapping expression persisted on the domain edge.
/// </summary>
public sealed partial class ConnectionViewModel : ObservableObject
{
    public ConnectionViewModel(PortViewModel source, PortViewModel target, string? mapping = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        Source = source;
        Target = target;
        _mapping = mapping;
    }

    /// <summary>The originating output port.</summary>
    public PortViewModel Source { get; }

    /// <summary>The terminating input port.</summary>
    public PortViewModel Target { get; }

    /// <summary>Optional data-mapping expression (e.g. <c>{{Login.token}}</c>).</summary>
    [ObservableProperty]
    private string? _mapping;
}
