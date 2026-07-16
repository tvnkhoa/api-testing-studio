using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Workflows;

/// <summary>A lightweight workflow summary for list rendering (no graph loaded).</summary>
public sealed record WorkflowListItem(Guid Id, string Name, string? Description);

/// <summary>
/// UI-facing CRUD over workflows for the Workflows tool panel: list/create/rename/delete, returning
/// <see cref="Result"/> for recoverable failures. Delegates persistence to <c>IWorkflowRepository</c>;
/// the designer itself loads/saves the full graph through that repository directly.
/// </summary>
public interface IWorkflowCatalogService
{
    /// <summary>Lists the workflows in a workspace (name-ordered), without their graphs.</summary>
    Task<Result<IReadOnlyList<WorkflowListItem>>> ListAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>Creates an empty workflow with the given name and returns its summary.</summary>
    Task<Result<WorkflowListItem>> CreateAsync(Guid workspaceId, string name, CancellationToken cancellationToken = default);

    /// <summary>Renames a workflow, preserving its graph.</summary>
    Task<Result> RenameAsync(Guid id, string name, CancellationToken cancellationToken = default);

    /// <summary>Deletes a workflow and its graph.</summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
