using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Workflows;

/// <inheritdoc />
public sealed class WorkflowCatalogService : IWorkflowCatalogService
{
    private readonly IWorkflowRepository _repository;

    public WorkflowCatalogService(IWorkflowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<Result<IReadOnlyList<WorkflowListItem>>> ListAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var definitions = await _repository.ListAsync(workspaceId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<WorkflowListItem> items = definitions
            .Select(d => new WorkflowListItem(d.Id, d.Name, d.Description))
            .ToList();
        return Result.Success(items);
    }

    public async Task<Result<WorkflowListItem>> CreateAsync(Guid workspaceId, string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<WorkflowListItem>(WorkflowErrors.NameRequired);
        }

        var workflow = new Workflow
        {
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            Nodes = [],
            Edges = [],
        };
        await _repository.SaveAsync(workflow, cancellationToken).ConfigureAwait(false);
        return Result.Success(new WorkflowListItem(workflow.Id, workflow.Name, workflow.Description));
    }

    public async Task<Result> RenameAsync(Guid id, string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(WorkflowErrors.NameRequired);
        }

        var workflow = await _repository.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (workflow is null)
        {
            return Result.Failure(WorkflowErrors.NotFound(id));
        }

        await _repository.SaveAsync(workflow with { Name = name.Trim() }, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
