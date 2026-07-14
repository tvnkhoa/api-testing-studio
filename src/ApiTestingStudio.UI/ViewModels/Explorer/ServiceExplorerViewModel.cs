using System.Collections.ObjectModel;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.ServiceCatalog;
using ApiTestingStudio.Shared.Results;
using ApiTestingStudio.UI.Messaging;
using ApiTestingStudio.UI.Services;
using ApiTestingStudio.UI.ViewModels.Panels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace ApiTestingStudio.UI.ViewModels.Explorer;

/// <summary>
/// The Service Explorer tool panel: a Service → Folder → Endpoint tree with search, CRUD, reorder
/// and per-workspace expansion/selection persistence. Selecting an endpoint publishes an
/// <see cref="EndpointSelectedMessage"/> for the API Runner (Sprint 06). All real work is delegated
/// to application services; this view model only orchestrates.
/// </summary>
public sealed partial class ServiceExplorerViewModel : ToolPanelViewModel, IExplorerNodeHost
{
    public const string PanelContentId = "tool.explorer";

    private readonly IServiceExplorerService _explorer;
    private readonly IEndpointCrudService _endpoints;
    private readonly IServiceExplorerStateService _state;
    private readonly IMessenger _messenger;
    private readonly IDialogService _dialog;
    private readonly IWorkspaceSession _session;
    private readonly IStatusBarService _statusBar;
    private readonly ILogger<ServiceExplorerViewModel> _logger;

    private bool _suppressPersist;
    private Guid? _pendingSelectionId;

    public ServiceExplorerViewModel(
        IServiceExplorerService explorer,
        IEndpointCrudService endpoints,
        IServiceExplorerStateService state,
        IMessenger messenger,
        IDialogService dialog,
        IWorkspaceSession session,
        IStatusBarService statusBar,
        ILogger<ServiceExplorerViewModel> logger)
        : base(PanelContentId, "Explorer")
    {
        ArgumentNullException.ThrowIfNull(explorer);
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(dialog);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(statusBar);
        ArgumentNullException.ThrowIfNull(logger);

        _explorer = explorer;
        _endpoints = endpoints;
        _state = state;
        _messenger = messenger;
        _dialog = dialog;
        _session = session;
        _statusBar = statusBar;
        _logger = logger;
    }

    /// <summary>Root service nodes bound to the tree.</summary>
    public ObservableCollection<ExplorerNodeViewModel> Roots { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddServiceCommand))]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    private bool _isWorkspaceOpen;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddEndpointCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(DuplicateCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveDownCommand))]
    private ExplorerNodeViewModel? _selectedNode;

    /// <summary>Loads (or reloads) the tree for the open workspace and restores its saved state.</summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsWorkspaceOpen = _session.IsOpen;
        if (!_session.IsOpen)
        {
            Clear();
            return;
        }

        var result = await _explorer.LoadTreeAsync(cancellationToken).ConfigureAwait(true);
        if (result.IsFailure)
        {
            Clear();
            _statusBar.SetMessage(result.Error.Message);
            return;
        }

        _suppressPersist = true;
        Roots.Clear();
        SelectedNode = null;
        foreach (var service in result.Value.Services)
        {
            Roots.Add(BuildServiceNode(service));
        }

        var saved = await _state.LoadAsync(cancellationToken).ConfigureAwait(true);
        RestoreState([.. saved.ExpandedIds], _pendingSelectionId ?? saved.SelectedId);
        ApplyFilter();
        _pendingSelectionId = null;
        _suppressPersist = false;
    }

    /// <summary>Empties the tree (used when the workspace closes).</summary>
    public void Clear()
    {
        _suppressPersist = true;
        Roots.Clear();
        SelectedNode = null;
        IsWorkspaceOpen = _session.IsOpen;
        _suppressPersist = false;
    }

    void IExplorerNodeHost.OnNodeSelected(ExplorerNodeViewModel node)
    {
        SelectedNode = node;

        if (_suppressPersist)
        {
            return;
        }

        if (node is EndpointNodeViewModel endpoint)
        {
            _messenger.Send(new EndpointSelectedMessage(endpoint.Id, endpoint.ServiceId, endpoint.Name, endpoint.Method, endpoint.Path));
            _statusBar.SetMessage($"Selected {endpoint.MethodLabel} {endpoint.Path}");
        }

        PersistState();
    }

    void IExplorerNodeHost.OnNodeExpansionChanged(ExplorerNodeViewModel node)
    {
        if (!_suppressPersist)
        {
            PersistState();
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand(CanExecute = nameof(IsWorkspaceOpen))]
    private Task RefreshAsync(CancellationToken cancellationToken) => LoadAsync(cancellationToken);

    [RelayCommand(CanExecute = nameof(IsWorkspaceOpen))]
    private async Task AddServiceAsync(CancellationToken cancellationToken)
    {
        var draft = _dialog.PromptService("New Service");
        if (draft is null)
        {
            return;
        }

        var result = await _explorer.CreateServiceAsync(draft, cancellationToken).ConfigureAwait(true);
        await AfterMutationAsync(result, result.IsSuccess ? result.Value.Id : null, cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanAddChild))]
    private async Task AddFolderAsync(CancellationToken cancellationToken)
    {
        if (!TryResolveParent(SelectedNode, out var serviceId, out var parentFolderId))
        {
            return;
        }

        var name = _dialog.PromptName("New Folder", "Folder name");
        if (name is null)
        {
            return;
        }

        var result = await _explorer.CreateFolderAsync(serviceId, parentFolderId, name, cancellationToken).ConfigureAwait(true);
        await AfterMutationAsync(result, result.IsSuccess ? result.Value.Id : null, cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanAddChild))]
    private async Task AddEndpointAsync(CancellationToken cancellationToken)
    {
        if (!TryResolveParent(SelectedNode, out var serviceId, out var folderId))
        {
            return;
        }

        var draft = _dialog.PromptEndpoint("New Endpoint");
        if (draft is null)
        {
            return;
        }

        var result = await _endpoints.CreateEndpointAsync(serviceId, folderId, draft, cancellationToken).ConfigureAwait(true);
        await AfterMutationAsync(result, result.IsSuccess ? result.Value.Id : null, cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task EditAsync(CancellationToken cancellationToken)
    {
        switch (SelectedNode)
        {
            case ServiceNodeViewModel service:
                var serviceDraft = _dialog.PromptService("Edit Service", new ServiceDraft(service.Name, service.BaseUrl, service.Description));
                if (serviceDraft is not null)
                {
                    var r = await _explorer.UpdateServiceAsync(service.Id, serviceDraft, cancellationToken).ConfigureAwait(true);
                    await AfterMutationAsync(r, service.Id, cancellationToken).ConfigureAwait(true);
                }

                break;

            case FolderNodeViewModel folder:
                var name = _dialog.PromptName("Rename Folder", "Folder name", folder.Name);
                if (name is not null)
                {
                    var r = await _explorer.RenameFolderAsync(folder.Id, name, cancellationToken).ConfigureAwait(true);
                    await AfterMutationAsync(r, folder.Id, cancellationToken).ConfigureAwait(true);
                }

                break;

            case EndpointNodeViewModel endpoint:
                var endpointDraft = _dialog.PromptEndpoint(
                    "Edit Endpoint",
                    new EndpointDraft(endpoint.Name, endpoint.Method, endpoint.Path, endpoint.Description));
                if (endpointDraft is not null)
                {
                    var r = await _endpoints.UpdateEndpointAsync(endpoint.Id, endpointDraft, cancellationToken).ConfigureAwait(true);
                    await AfterMutationAsync(r, endpoint.Id, cancellationToken).ConfigureAwait(true);
                }

                break;
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task DeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedNode is not { } node)
        {
            return;
        }

        if (!_dialog.Confirm("Delete", $"Delete '{node.Name}' and everything it contains?"))
        {
            return;
        }

        var result = node switch
        {
            ServiceNodeViewModel s => await _explorer.DeleteServiceAsync(s.Id, cancellationToken).ConfigureAwait(true),
            FolderNodeViewModel f => await _explorer.DeleteFolderAsync(f.Id, cancellationToken).ConfigureAwait(true),
            EndpointNodeViewModel e => await _endpoints.DeleteEndpointAsync(e.Id, cancellationToken).ConfigureAwait(true),
            _ => Result.Success(),
        };

        await AfterMutationAsync(result, null, cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanDuplicate))]
    private async Task DuplicateAsync(CancellationToken cancellationToken)
    {
        if (SelectedNode is not EndpointNodeViewModel endpoint)
        {
            return;
        }

        var result = await _endpoints.DuplicateEndpointAsync(endpoint.Id, cancellationToken).ConfigureAwait(true);
        await AfterMutationAsync(result, result.IsSuccess ? result.Value.Id : null, cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private Task MoveUpAsync(CancellationToken cancellationToken) => ReorderAsync(up: true, cancellationToken);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private Task MoveDownAsync(CancellationToken cancellationToken) => ReorderAsync(up: false, cancellationToken);

    private async Task ReorderAsync(bool up, CancellationToken cancellationToken)
    {
        if (SelectedNode is not { } node)
        {
            return;
        }

        var result = node switch
        {
            ServiceNodeViewModel s => await _explorer.ReorderServiceAsync(s.Id, up, cancellationToken).ConfigureAwait(true),
            FolderNodeViewModel f => await _explorer.ReorderFolderAsync(f.Id, up, cancellationToken).ConfigureAwait(true),
            EndpointNodeViewModel e => await _endpoints.ReorderEndpointAsync(e.Id, up, cancellationToken).ConfigureAwait(true),
            _ => Result.Success(),
        };

        await AfterMutationAsync(result, node.Id, cancellationToken).ConfigureAwait(true);
    }

    private bool CanAddChild() => SelectedNode is ServiceNodeViewModel or FolderNodeViewModel;

    private bool HasSelection() => SelectedNode is not null;

    private bool CanDuplicate() => SelectedNode is EndpointNodeViewModel;

    private async Task AfterMutationAsync(Result result, Guid? selectId, CancellationToken cancellationToken)
    {
        if (result.IsFailure)
        {
            _statusBar.SetMessage(result.Error.Message);
            return;
        }

        _pendingSelectionId = selectId;
        await LoadAsync(cancellationToken).ConfigureAwait(true);
    }

    private Task AfterMutationAsync<T>(Result<T> result, Guid? selectId, CancellationToken cancellationToken)
        => AfterMutationAsync(result.IsSuccess ? Result.Success() : Result.Failure(result.Error), selectId, cancellationToken);

    private ServiceNodeViewModel BuildServiceNode(ServiceNode service)
    {
        var node = new ServiceNodeViewModel(this, service.Id, service.Name, service.BaseUrl, service.Description);
        foreach (var folder in service.Folders)
        {
            node.Children.Add(BuildFolderNode(folder));
        }

        foreach (var endpoint in service.Endpoints)
        {
            node.Children.Add(BuildEndpointNode(endpoint));
        }

        return node;
    }

    private FolderNodeViewModel BuildFolderNode(FolderNode folder)
    {
        var node = new FolderNodeViewModel(this, folder.Id, folder.ServiceId, folder.ParentFolderId, folder.Name);
        foreach (var sub in folder.Folders)
        {
            node.Children.Add(BuildFolderNode(sub));
        }

        foreach (var endpoint in folder.Endpoints)
        {
            node.Children.Add(BuildEndpointNode(endpoint));
        }

        return node;
    }

    private EndpointNodeViewModel BuildEndpointNode(EndpointNode endpoint)
        => new(this, endpoint.Id, endpoint.ServiceId, endpoint.FolderId, endpoint.Name, endpoint.Method, endpoint.Path, endpoint.Description);

    private void RestoreState(HashSet<Guid> expandedIds, Guid? selectedId)
    {
        foreach (var node in EnumerateNodes(Roots))
        {
            if (expandedIds.Contains(node.Id))
            {
                node.IsExpanded = true;
            }

            if (selectedId is { } id && node.Id == id)
            {
                node.IsSelected = true;
                SelectedNode = node;
            }
        }
    }

    private void ApplyFilter()
    {
        var wasSuppressed = _suppressPersist;
        _suppressPersist = true;
        foreach (var root in Roots)
        {
            FilterNode(root, SearchText);
        }

        _suppressPersist = wasSuppressed;
    }

    private static bool FilterNode(ExplorerNodeViewModel node, string? query)
    {
        var childMatch = false;
        foreach (var child in node.Children)
        {
            childMatch |= FilterNode(child, query);
        }

        var selfMatch = ServiceCatalogSearch.Matches(node.Name, query);
        node.IsVisible = selfMatch || childMatch;

        if (childMatch && !string.IsNullOrWhiteSpace(query))
        {
            node.IsExpanded = true;
        }

        return node.IsVisible;
    }

    private void PersistState()
    {
        var expanded = EnumerateNodes(Roots).Where(n => n.IsExpanded).Select(n => n.Id).ToList();
        var state = new ExplorerTreeState(expanded, SelectedNode?.Id);
        _ = SaveStateAsync(state);
    }

    private async Task SaveStateAsync(ExplorerTreeState state)
    {
        try
        {
            await _state.SaveAsync(state).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist Service Explorer tree state.");
        }
    }

    private static bool TryResolveParent(ExplorerNodeViewModel? node, out Guid serviceId, out Guid? folderId)
    {
        switch (node)
        {
            case ServiceNodeViewModel service:
                serviceId = service.Id;
                folderId = null;
                return true;
            case FolderNodeViewModel folder:
                serviceId = folder.ServiceId;
                folderId = folder.Id;
                return true;
            default:
                serviceId = Guid.Empty;
                folderId = null;
                return false;
        }
    }

    private static IEnumerable<ExplorerNodeViewModel> EnumerateNodes(IEnumerable<ExplorerNodeViewModel> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var descendant in EnumerateNodes(node.Children))
            {
                yield return descendant;
            }
        }
    }
}
