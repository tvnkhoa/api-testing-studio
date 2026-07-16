using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTestingStudio.UI.ViewModels.Workflow;

/// <summary>Which side of a node a port lives on.</summary>
public enum PortDirection
{
    Input,
    Output,
}

/// <summary>
/// A single input/output port on a node — the data context of a Nodify <c>Connector</c>. <see
/// cref="Anchor"/> is written back by Nodify (OneWayToSource) as the node moves, so connections can
/// track it. <see cref="Key"/> is the engine-level port name (see <c>WorkflowPorts</c>).
/// </summary>
public sealed partial class PortViewModel : ObservableObject
{
    public PortViewModel(NodeViewModel node, string key, PortDirection direction)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        Node = node;
        Key = key;
        Direction = direction;
    }

    /// <summary>The node this port belongs to.</summary>
    public NodeViewModel Node { get; }

    /// <summary>The engine port name (e.g. <c>next</c>, <c>true</c>, <c>false</c>, <c>body</c>, <c>in</c>).</summary>
    public string Key { get; }

    /// <summary>Input or output side.</summary>
    public PortDirection Direction { get; }

    /// <summary>Display title shown next to the connector.</summary>
    public string Title => Key;

    /// <summary>Graph-space attach point, written by the Nodify connector as the node moves.</summary>
    [ObservableProperty]
    private Point _anchor;

    /// <summary>Whether at least one connection is attached (drives the connector's filled state).</summary>
    [ObservableProperty]
    private bool _isConnected;
}
