using System.Collections.ObjectModel;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.UI.Messaging;
using ApiTestingStudio.UI.Services;
using ApiTestingStudio.UI.ViewModels.Panels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace ApiTestingStudio.UI.ViewModels.Workflow;

/// <summary>
/// The Workflows tool panel: lists the open workspace's workflows and offers New/Rename/Delete.
/// Selecting a workflow publishes an <see cref="OpenWorkflowMessage"/> for the shell to open (or
/// focus) its designer document pane — mirroring the Service Explorer → Runner selection path. All
/// work is delegated to <see cref="IWorkflowCatalogService"/>.
/// </summary>
public sealed partial class WorkflowsPanelViewModel : ToolPanelViewModel
{
    public const string PanelContentId = "tool.workflows";

    private readonly IWorkflowCatalogService _catalog;
    private readonly IMessenger _messenger;
    private readonly IDialogService _dialog;
    private readonly IWorkspaceSession _session;
    private readonly IStatusBarService _statusBar;
    private readonly ILogger<WorkflowsPanelViewModel> _logger;

    private bool _suppressOpen;

    public WorkflowsPanelViewModel(
        IWorkflowCatalogService catalog,
        IMessenger messenger,
        IDialogService dialog,
        IWorkspaceSession session,
        IStatusBarService statusBar,
        ILogger<WorkflowsPanelViewModel> logger)
        : base(PanelContentId, "Workflows")
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(dialog);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(statusBar);
        ArgumentNullException.ThrowIfNull(logger);

        _catalog = catalog;
        _messenger = messenger;
        _dialog = dialog;
        _session = session;
        _statusBar = statusBar;
        _logger = logger;
    }

    /// <summary>The workspace's workflows, name-ordered.</summary>
    public ObservableCollection<WorkflowListItem> Workflows { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NewCommand))]
    private bool _isWorkspaceOpen;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RenameCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenCommand))]
    private WorkflowListItem? _selectedWorkflow;

    /// <summary>Loads (or reloads) the workflow list for the open workspace.</summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsWorkspaceOpen = _session.IsOpen;
        if (!_session.IsOpen || _session.Current is not { } workspace)
        {
            Clear();
            return;
        }

        var result = await _catalog.ListAsync(workspace.Id, cancellationToken).ConfigureAwait(true);
        _suppressOpen = true;
        Workflows.Clear();
        SelectedWorkflow = null;
        _suppressOpen = false;

        if (result.IsFailure)
        {
            _statusBar.SetMessage(result.Error.Message);
            return;
        }

        foreach (var item in result.Value)
        {
            Workflows.Add(item);
        }
    }

    /// <summary>Empties the list (used when the workspace closes).</summary>
    public void Clear()
    {
        _suppressOpen = true;
        Workflows.Clear();
        SelectedWorkflow = null;
        IsWorkspaceOpen = _session.IsOpen;
        _suppressOpen = false;
    }

    partial void OnSelectedWorkflowChanged(WorkflowListItem? value)
    {
        if (!_suppressOpen && value is not null)
        {
            _messenger.Send(new OpenWorkflowMessage(value.Id, value.Name));
        }
    }

    [RelayCommand(CanExecute = nameof(IsWorkspaceOpen))]
    private async Task NewAsync(CancellationToken cancellationToken)
    {
        if (_session.Current is not { } workspace)
        {
            return;
        }

        var name = _dialog.PromptName("New Workflow", "Name");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var result = await _catalog.CreateAsync(workspace.Id, name, cancellationToken).ConfigureAwait(true);
        if (result.IsFailure)
        {
            _statusBar.SetMessage(result.Error.Message);
            return;
        }

        await LoadAsync(cancellationToken).ConfigureAwait(true);
        SelectedWorkflow = Workflows.FirstOrDefault(w => w.Id == result.Value.Id);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task RenameAsync(CancellationToken cancellationToken)
    {
        if (SelectedWorkflow is not { } workflow)
        {
            return;
        }

        var name = _dialog.PromptName("Rename Workflow", "Name", workflow.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var result = await _catalog.RenameAsync(workflow.Id, name, cancellationToken).ConfigureAwait(true);
        if (result.IsFailure)
        {
            _statusBar.SetMessage(result.Error.Message);
            return;
        }

        await LoadAsync(cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task DeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedWorkflow is not { } workflow)
        {
            return;
        }

        if (!_dialog.Confirm("Delete", $"Delete workflow '{workflow.Name}'?"))
        {
            return;
        }

        var result = await _catalog.DeleteAsync(workflow.Id, cancellationToken).ConfigureAwait(true);
        if (result.IsFailure)
        {
            _statusBar.SetMessage(result.Error.Message);
            return;
        }

        await LoadAsync(cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Open()
    {
        if (SelectedWorkflow is { } workflow)
        {
            _messenger.Send(new OpenWorkflowMessage(workflow.Id, workflow.Name));
        }
    }

    private bool HasSelection() => SelectedWorkflow is not null;
}
