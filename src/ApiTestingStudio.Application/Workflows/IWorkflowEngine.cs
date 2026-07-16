using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Workflows;

/// <summary>
/// Headless workflow execution engine. Walks a <see cref="Workflow"/> graph, dispatches each node to
/// its <see cref="INodeHandler"/>, and returns a structured <see cref="WorkflowRunResult"/>. Pure and
/// UI-independent; reused by the Designer (Sprint 09) and Dashboard (Sprint 13).
/// </summary>
public interface IWorkflowEngine
{
    /// <summary>
    /// Runs <paramref name="workflow"/> to completion. When <paramref name="context"/> is null a fresh
    /// one is created (seed variables by passing your own). Cancellation stops the run promptly and
    /// yields a <c>Cancelled</c> result. When <paramref name="progress"/> is supplied, each node is
    /// reported as it starts (a <c>Running</c> <see cref="NodeRunResult"/>) and again with its final
    /// result, so a UI (the Sprint 09 designer) can reflect live per-node status.
    /// </summary>
    Task<WorkflowRunResult> RunAsync(
        Workflow workflow,
        WorkflowRunOptions? options = null,
        IWorkflowContext? context = null,
        IProgress<NodeRunResult>? progress = null,
        CancellationToken cancellationToken = default);
}
