using System.Collections.ObjectModel;
using System.Windows;
using ApiTestingStudio.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTestingStudio.UI.ViewModels.Workflow;

/// <summary>
/// The designer view model for one workflow node — the data context of a Nodify <c>ItemContainer</c>.
/// <see cref="Location"/> binds two-way to the container; <see cref="Status"/> drives live run
/// colouring. <see cref="Config"/> holds the typed <c>*NodeConfig</c> record edited by the property
/// inspector and serialized back to the domain node by <see cref="GraphMapper"/>.
/// </summary>
public sealed partial class NodeViewModel : ObservableObject
{
    public NodeViewModel(Guid id, WorkflowNodeKind kind, string title)
    {
        Id = id;
        Kind = kind;
        _title = title;
    }

    /// <summary>Stable node id, shared with the domain <c>WorkflowNode</c>.</summary>
    public Guid Id { get; }

    /// <summary>The node kind (fixed once created).</summary>
    public WorkflowNodeKind Kind { get; }

    /// <summary>Raised when this node becomes selected, so the editor can drive the inspector.</summary>
    public event Action<NodeViewModel>? Selected;

    /// <summary>Input ports (left side).</summary>
    public ObservableCollection<PortViewModel> Input { get; } = [];

    /// <summary>Output ports (right side).</summary>
    public ObservableCollection<PortViewModel> Output { get; } = [];

    /// <summary>The typed configuration record for this kind (e.g. <c>RequestNodeConfig</c>), or null.</summary>
    public object? Config { get; set; }

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private Point _location;

    [ObservableProperty]
    private double? _width;

    [ObservableProperty]
    private double? _height;

    [ObservableProperty]
    private string? _color;

    [ObservableProperty]
    private RunStatus _status = RunStatus.Pending;

    [ObservableProperty]
    private bool _isSelected;

    partial void OnIsSelectedChanged(bool value)
    {
        if (value)
        {
            Selected?.Invoke(this);
        }
    }
}
