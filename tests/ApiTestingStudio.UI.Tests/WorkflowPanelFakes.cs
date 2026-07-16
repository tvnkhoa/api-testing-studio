using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Shared.Results;
using ApiTestingStudio.UI.ViewModels.Workflow;

namespace ApiTestingStudio.UI.Tests;

/// <summary>In-memory workflow catalog for shell/panel tests.</summary>
internal sealed class FakeWorkflowCatalogService : IWorkflowCatalogService
{
    public List<WorkflowListItem> Items { get; } = [];

    public Task<Result<IReadOnlyList<WorkflowListItem>>> ListAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success<IReadOnlyList<WorkflowListItem>>(Items));

    public Task<Result<WorkflowListItem>> CreateAsync(Guid workspaceId, string name, CancellationToken cancellationToken = default)
    {
        var item = new WorkflowListItem(Guid.NewGuid(), name, null);
        Items.Add(item);
        return Task.FromResult(Result.Success(item));
    }

    public Task<Result> RenameAsync(Guid id, string name, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());

    public Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());
}

/// <summary>Factory stub — the shell tests never open a designer pane, so Create is unused.</summary>
internal sealed class FakeWorkflowEditorViewModelFactory : IWorkflowEditorViewModelFactory
{
    public WorkflowEditorViewModel Create(Guid workflowId, string name)
        => throw new NotSupportedException("The designer factory is not exercised by these tests.");
}
