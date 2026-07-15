using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Persists workflow graphs (definition + nodes + edges) for the currently open workspace and
/// hydrates them into a runnable <see cref="Workflow"/> aggregate. Operations require an open
/// workspace. EF Core types never cross this port. See <c>.claude/DATABASE_GUIDELINES.md</c>.
/// </summary>
public interface IWorkflowRepository
{
    /// <summary>Loads a workflow with its full node/edge graph, or null when not found.</summary>
    Task<Workflow?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Lists the workflow definitions (without graphs) in a workspace.</summary>
    Task<IReadOnlyList<WorkflowDefinition>> ListAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>Inserts or updates a workflow and replaces its node/edge graph.</summary>
    Task SaveAsync(Workflow workflow, CancellationToken cancellationToken = default);

    /// <summary>Deletes a workflow and its nodes/edges.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
