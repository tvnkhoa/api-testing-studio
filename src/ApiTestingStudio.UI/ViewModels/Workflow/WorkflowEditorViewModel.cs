using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Common;
using ApiTestingStudio.Application.Runs;
using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.UI.Services;
using ApiTestingStudio.UI.ViewModels.Panels;
using ApiTestingStudio.UI.ViewModels.Workflow.Commands;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace ApiTestingStudio.UI.ViewModels.Workflow;

/// <summary>
/// The Workflow Designer document pane: hosts the Nodify canvas for one workflow. It edits the same
/// domain graph the Sprint 08 engine executes (via <see cref="GraphMapper"/>), records every edit on
/// a shared <see cref="IUndoRedoService"/>, validates connections through <see cref="IConnectorValidator"/>,
/// and runs the workflow with live per-node status streamed from the engine's <c>IProgress</c>.
/// One instance per open workflow (stable <see cref="PanelViewModel.ContentId"/>).
/// </summary>
public sealed partial class WorkflowEditorViewModel : DocumentPanelViewModel
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Guid _workflowId;
    private readonly IWorkflowRepository _repository;
    private readonly IWorkflowEngine _engine;
    private readonly IUndoRedoService _undo;
    private readonly IConnectorValidator _validator;
    private readonly INodeViewModelFactory _nodeFactory;
    private readonly GraphMapper _mapper;
    private readonly IStatusBarService _statusBar;
    private readonly IRunRecorder _runRecorder;
    private readonly IEndpointRepository _endpoints;
    private readonly IServiceRepository _services;
    private readonly ILogger<WorkflowEditorViewModel> _logger;

    private readonly Dictionary<NodeViewModel, Point> _dragStart = [];

    private Guid _workspaceId;
    private string? _description;

    public WorkflowEditorViewModel(
        Guid workflowId,
        string name,
        IWorkflowRepository repository,
        IWorkflowEngine engine,
        IUndoRedoService undo,
        IConnectorValidator validator,
        INodeViewModelFactory nodeFactory,
        GraphMapper mapper,
        IStatusBarService statusBar,
        IRunRecorder runRecorder,
        IEndpointRepository endpoints,
        IServiceRepository services,
        ILogger<WorkflowEditorViewModel> logger)
        : base($"document.workflow.{workflowId}", name)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(undo);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(nodeFactory);
        ArgumentNullException.ThrowIfNull(mapper);
        ArgumentNullException.ThrowIfNull(statusBar);
        ArgumentNullException.ThrowIfNull(runRecorder);
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        _workflowId = workflowId;
        _repository = repository;
        _engine = engine;
        _undo = undo;
        _validator = validator;
        _nodeFactory = nodeFactory;
        _mapper = mapper;
        _statusBar = statusBar;
        _runRecorder = runRecorder;
        _endpoints = endpoints;
        _services = services;
        _logger = logger;

        Properties = new NodePropertiesViewModel(undo);
        _undo.StateChanged += OnUndoStateChanged;
        Nodes.CollectionChanged += (_, _) => RunCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Nodes bound to the Nodify editor's <c>ItemsSource</c>.</summary>
    public ObservableCollection<NodeViewModel> Nodes { get; } = [];

    /// <summary>Connections bound to the Nodify editor's <c>Connections</c>.</summary>
    public ObservableCollection<ConnectionViewModel> Connections { get; } = [];

    /// <summary>The draggable node toolbox.</summary>
    public NodePaletteViewModel Palette { get; } = new();

    /// <summary>The property inspector bound to <see cref="SelectedNode"/>.</summary>
    public NodePropertiesViewModel Properties { get; }

    [ObservableProperty]
    private NodeViewModel? _selectedNode;

    [ObservableProperty]
    private bool _isRunning;

    /// <summary>Loads the workflow graph from storage into the canvas.</summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var workflow = await _repository.GetAsync(_workflowId, cancellationToken).ConfigureAwait(true);
        if (workflow is null)
        {
            _statusBar.SetMessage("The workflow could not be loaded.");
            return;
        }

        _workspaceId = workflow.WorkspaceId;
        _description = workflow.Description;
        Title = workflow.Name;

        ClearGraph();
        var (nodes, connections) = _mapper.ToViewModel(workflow);
        foreach (var node in nodes)
        {
            AttachNode(node);
        }

        foreach (var connection in connections)
        {
            Connections.Add(connection);
        }

        RecomputeConnectivity();
        _undo.Clear();
        SelectedNode = null;
    }

    /// <summary>Creates a node of <paramref name="kind"/> at a canvas point (used by palette drag-drop).</summary>
    public void AddNodeAt(WorkflowNodeKind kind, Point location)
    {
        var node = _nodeFactory.Create(kind, location);
        node.Selected += OnNodeSelected;
        _undo.Execute(new AddNodeCommand(Nodes, node, RecomputeConnectivity));
    }

    /// <summary>
    /// Creates an <see cref="WorkflowNodeKind.Api"/> node at a canvas point, pre-filled from a saved
    /// endpoint (method, resolved URL, default headers and body). Used when an endpoint is dropped
    /// from the Service Explorer. Resolution failures are surfaced on the status bar, not thrown, so
    /// the caller can fire-and-forget from the drop handler.
    /// </summary>
    public async Task AddApiNodeFromEndpointAsync(Guid endpointId, Point location, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = await _endpoints.GetAsync(endpointId, cancellationToken).ConfigureAwait(true);
            if (endpoint is null)
            {
                _statusBar.SetMessage("The dropped endpoint could not be found.");
                return;
            }

            var service = await _services.GetAsync(endpoint.ServiceId, cancellationToken).ConfigureAwait(true);

            var node = _nodeFactory.Create(WorkflowNodeKind.Api, location);
            node.Title = endpoint.Name;
            node.Config = new RequestNodeConfig
            {
                Method = endpoint.Method,
                Url = CombineUrl(service?.BaseUrl, endpoint.Path),
                Headers = DeserializeHeaders(endpoint.DefaultHeaders),
                BodyKind = BodyKind.Json,
                Body = endpoint.DefaultBody,
            };
            node.Selected += OnNodeSelected;

            _undo.Execute(new AddNodeCommand(Nodes, node, RecomputeConnectivity));
            _statusBar.SetMessage($"Added '{endpoint.Name}' as an API node.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add endpoint {EndpointId} to workflow {WorkflowId}.", endpointId, _workflowId);
            _statusBar.SetMessage("Failed to add the dropped endpoint.");
        }
    }

    partial void OnSelectedNodeChanged(NodeViewModel? value) => Properties.Load(value);

    [RelayCommand]
    private void AddNode(WorkflowNodeKind kind)
    {
        // Palette click (no drop point): cascade new nodes so they don't stack exactly.
        var offset = 40 + (Nodes.Count % 8 * 28);
        AddNodeAt(kind, new Point(offset, offset));
    }

    [RelayCommand(CanExecute = nameof(HasSelectedNode))]
    private void DeleteSelection()
    {
        if (SelectedNode is not { } node)
        {
            return;
        }

        node.Selected -= OnNodeSelected;
        _undo.Execute(new RemoveNodeCommand(Nodes, Connections, node, RecomputeConnectivity));
        SelectedNode = null;
    }

    [RelayCommand]
    private void Connect(object? parameter)
    {
        if (parameter is not Tuple<object, object> pair
            || pair.Item1 is not PortViewModel source
            || pair.Item2 is not PortViewModel target)
        {
            return;
        }

        var request = new ConnectionRequest
        {
            SourceNodeId = source.Node.Id,
            SourceKind = source.Node.Kind,
            SourcePort = source.Key,
            TargetNodeId = target.Node.Id,
            TargetKind = target.Node.Kind,
            TargetPort = target.Key,
            ExistingEdges = CurrentEdges(),
        };

        var result = _validator.Validate(request);
        if (result.IsFailure)
        {
            _statusBar.SetMessage(result.Error.Message);
            return;
        }

        _undo.Execute(new AddConnectionCommand(Connections, new ConnectionViewModel(source, target), RecomputeConnectivity));
    }

    [RelayCommand]
    private void DisconnectConnector(object? parameter)
    {
        if (parameter is not PortViewModel port)
        {
            return;
        }

        var removed = Connections.Where(c => c.Source == port || c.Target == port).ToList();
        if (removed.Count == 0)
        {
            return;
        }

        _undo.Execute(new RemoveConnectionsCommand(Connections, removed, RecomputeConnectivity));
    }

    [RelayCommand]
    private void RemoveConnection(object? parameter)
    {
        if (parameter is not ConnectionViewModel connection)
        {
            return;
        }

        _undo.Execute(new RemoveConnectionsCommand(Connections, [connection], RecomputeConnectivity));
    }

    [RelayCommand]
    private void ItemsDragStarted()
    {
        _dragStart.Clear();
        foreach (var node in Nodes)
        {
            _dragStart[node] = node.Location;
        }
    }

    [RelayCommand]
    private void ItemsDragCompleted()
    {
        var moves = Nodes
            .Where(n => _dragStart.TryGetValue(n, out var from) && from != n.Location)
            .Select(n => (Node: n, From: _dragStart[n], To: n.Location))
            .ToList();
        _dragStart.Clear();

        if (moves.Count > 0)
        {
            _undo.Execute(new MoveNodesCommand(moves));
        }
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo() => _undo.Undo();

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo() => _undo.Redo();

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        var workflow = GraphMapper.ToDomain(_workflowId, _workspaceId, Title, _description, Nodes, Connections);
        try
        {
            await _repository.SaveAsync(workflow, cancellationToken).ConfigureAwait(true);
            _statusBar.SetMessage($"Saved '{Title}'.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save workflow {WorkflowId}.", _workflowId);
            _statusBar.SetMessage("Failed to save the workflow.");
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        foreach (var node in Nodes)
        {
            node.Status = RunStatus.Pending;
        }

        var workflow = GraphMapper.ToDomain(_workflowId, _workspaceId, Title, _description, Nodes, Connections);
        var progress = new Progress<NodeRunResult>(OnNodeProgress);

        IsRunning = true;
        RunCommand.NotifyCanExecuteChanged();
        try
        {
            var result = await _engine
                .RunAsync(workflow, progress: progress, cancellationToken: cancellationToken)
                .ConfigureAwait(true);
            await _runRecorder.RecordWorkflowAsync(workflow, result, cancellationToken).ConfigureAwait(true);
            _statusBar.SetMessage($"Run {result.Status}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow run failed for {WorkflowId}.", _workflowId);
            _statusBar.SetMessage("The workflow run failed.");
        }
        finally
        {
            IsRunning = false;
            RunCommand.NotifyCanExecuteChanged();
        }
    }

    private void OnNodeProgress(NodeRunResult result)
    {
        var node = Nodes.FirstOrDefault(n => n.Id == result.NodeId);
        if (node is not null)
        {
            node.Status = result.Status;
        }
    }

    private void OnNodeSelected(NodeViewModel node) => SelectedNode = node;

    private void OnUndoStateChanged(object? sender, EventArgs e)
    {
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    private void AttachNode(NodeViewModel node)
    {
        node.Selected += OnNodeSelected;
        Nodes.Add(node);
    }

    private void ClearGraph()
    {
        foreach (var node in Nodes)
        {
            node.Selected -= OnNodeSelected;
        }

        Connections.Clear();
        Nodes.Clear();
    }

    private void RecomputeConnectivity()
    {
        foreach (var node in Nodes)
        {
            foreach (var port in node.Input)
            {
                port.IsConnected = Connections.Any(c => c.Target == port);
            }

            foreach (var port in node.Output)
            {
                port.IsConnected = Connections.Any(c => c.Source == port);
            }
        }
    }

    private List<WorkflowEdge> CurrentEdges() =>
        Connections.Select(c => new WorkflowEdge
        {
            SourceNodeId = c.Source.Node.Id,
            TargetNodeId = c.Target.Node.Id,
            SourcePort = c.Source.Key,
            TargetPort = c.Target.Key,
        }).ToList();

    private static string CombineUrl(string? baseUrl, string path)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return path;
        }

        if (string.IsNullOrEmpty(path))
        {
            return baseUrl;
        }

        return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

    private static List<HttpHeader> DeserializeHeaders(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<HttpHeader>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private bool HasSelectedNode() => SelectedNode is not null;

    private bool CanUndo() => _undo.CanUndo;

    private bool CanRedo() => _undo.CanRedo;

    private bool CanRun() => !IsRunning && Nodes.Count > 0;

    partial void OnSelectedNodeChanged(NodeViewModel? oldValue, NodeViewModel? newValue)
        => DeleteSelectionCommand.NotifyCanExecuteChanged();

    partial void OnIsRunningChanged(bool value) => RunCommand.NotifyCanExecuteChanged();
}
