using Microsoft.Extensions.DependencyInjection;

namespace ApiTestingStudio.UI.ViewModels.Workflow;

/// <summary>
/// Creates a fresh <see cref="WorkflowEditorViewModel"/> per workflow (each designer pane is its own
/// instance with its own undo stack), resolving the view model's services from DI while supplying the
/// per-workflow id/name. The shell uses this to open a document pane for a selected workflow.
/// </summary>
public interface IWorkflowEditorViewModelFactory
{
    WorkflowEditorViewModel Create(Guid workflowId, string name);
}

/// <inheritdoc />
public sealed class WorkflowEditorViewModelFactory : IWorkflowEditorViewModelFactory
{
    private readonly IServiceProvider _services;

    public WorkflowEditorViewModelFactory(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public WorkflowEditorViewModel Create(Guid workflowId, string name) =>
        ActivatorUtilities.CreateInstance<WorkflowEditorViewModel>(_services, workflowId, name);
}
