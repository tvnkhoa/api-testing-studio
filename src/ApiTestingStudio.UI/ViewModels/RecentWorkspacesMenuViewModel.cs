using System.Collections.ObjectModel;
using ApiTestingStudio.Application.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ApiTestingStudio.UI.ViewModels;

/// <summary>
/// Builds the File → Open Recent submenu from <see cref="IRecentWorkspacesService"/> (Sprint 02).
/// Selecting an entry raises <see cref="OpenRequested"/>; the shell handles the actual open so all
/// workspace-lifecycle logic stays in one place. A stale entry can be removed via each item.
/// </summary>
public sealed partial class RecentWorkspacesMenuViewModel : ObservableObject
{
    private readonly IRecentWorkspacesService _recentWorkspaces;

    public RecentWorkspacesMenuViewModel(IRecentWorkspacesService recentWorkspaces)
    {
        ArgumentNullException.ThrowIfNull(recentWorkspaces);
        _recentWorkspaces = recentWorkspaces;
    }

    /// <summary>The recent-workspace menu items, most-recent-first.</summary>
    public ObservableCollection<RecentWorkspaceItemViewModel> Items { get; } = [];

    /// <summary>Whether any recent workspaces exist (drives an "empty" placeholder in the menu).</summary>
    [ObservableProperty]
    private bool _hasItems;

    /// <summary>Raised when the user picks a recent workspace to open, with its location.</summary>
    public event EventHandler<string>? OpenRequested;

    /// <summary>Reloads the list from the MRU store. Call after any workspace open/create/close.</summary>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var entries = await _recentWorkspaces.GetAllAsync(cancellationToken).ConfigureAwait(true);

        Items.Clear();
        foreach (var entry in entries)
        {
            Items.Add(new RecentWorkspaceItemViewModel(entry.Location, entry.Name, OnOpen, OnRemoveAsync));
        }

        HasItems = Items.Count > 0;
    }

    private void OnOpen(string location) => OpenRequested?.Invoke(this, location);

    private async Task OnRemoveAsync(string location)
    {
        await _recentWorkspaces.RemoveAsync(location).ConfigureAwait(true);
        await RefreshAsync().ConfigureAwait(true);
    }
}

/// <summary>One entry in the recent-workspaces submenu.</summary>
public sealed partial class RecentWorkspaceItemViewModel : ObservableObject
{
    private readonly Action<string> _open;
    private readonly Func<string, Task> _remove;

    public RecentWorkspaceItemViewModel(string location, string name, Action<string> open, Func<string, Task> remove)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(location);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(open);
        ArgumentNullException.ThrowIfNull(remove);
        Location = location;
        Name = name;
        _open = open;
        _remove = remove;
    }

    public string Location { get; }

    public string Name { get; }

    /// <summary>Header shown in the menu: display name plus its path.</summary>
    public string DisplayText => $"{Name}   ({Location})";

    [RelayCommand]
    private void Open() => _open(Location);

    [RelayCommand]
    private Task Remove() => _remove(Location);
}
