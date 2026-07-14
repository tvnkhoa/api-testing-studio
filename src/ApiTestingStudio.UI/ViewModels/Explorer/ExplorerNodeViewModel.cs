using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTestingStudio.UI.ViewModels.Explorer;

/// <summary>
/// Callbacks a tree node raises to its owning <c>ServiceExplorerViewModel</c>. Keeps selection and
/// expansion handling in one place instead of the view model subscribing to every node.
/// </summary>
public interface IExplorerNodeHost
{
    void OnNodeSelected(ExplorerNodeViewModel node);

    void OnNodeExpansionChanged(ExplorerNodeViewModel node);
}

/// <summary>
/// Base view model for a node in the Service Explorer tree. Bound to a <c>TreeViewItem</c>; the item
/// container style binds <see cref="IsExpanded"/>/<see cref="IsSelected"/> two-way, and
/// <see cref="IsVisible"/> drives search filtering.
/// </summary>
public abstract partial class ExplorerNodeViewModel : ObservableObject
{
    private readonly IExplorerNodeHost _host;

    protected ExplorerNodeViewModel(IExplorerNodeHost host, Guid id, string name)
    {
        ArgumentNullException.ThrowIfNull(host);
        _host = host;
        Id = id;
        _name = name;
    }

    /// <summary>Stable identifier of the underlying entity (used for state + selection tracking).</summary>
    public Guid Id { get; }

    /// <summary>Child folders and endpoints, in display order.</summary>
    public ObservableCollection<ExplorerNodeViewModel> Children { get; } = [];

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isVisible = true;

    partial void OnIsSelectedChanged(bool value)
    {
        if (value)
        {
            _host.OnNodeSelected(this);
        }
    }

    partial void OnIsExpandedChanged(bool value) => _host.OnNodeExpansionChanged(this);
}
