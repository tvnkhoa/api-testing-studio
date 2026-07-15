using System.Collections.Concurrent;
using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Workflows;

/// <summary>
/// Stores completed <see cref="WorkflowRunResult"/>s. Sprint 08 ships an in-memory implementation;
/// durable run-history tables are deferred to Sprint 13 (Dashboard).
/// </summary>
public interface IWorkflowRunStore
{
    /// <summary>Records a completed run.</summary>
    void Save(WorkflowRunResult result);

    /// <summary>Returns the recorded runs for a workflow, oldest first.</summary>
    IReadOnlyList<WorkflowRunResult> GetForWorkflow(Guid workflowId);
}

/// <summary>Process-lifetime, thread-safe <see cref="IWorkflowRunStore"/>. Runs are not persisted.</summary>
internal sealed class InMemoryWorkflowRunStore : IWorkflowRunStore
{
    private readonly ConcurrentDictionary<Guid, List<WorkflowRunResult>> _runs = new();

    public void Save(WorkflowRunResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var list = _runs.GetOrAdd(result.WorkflowId, _ => new List<WorkflowRunResult>());
        lock (list)
        {
            list.Add(result);
        }
    }

    public IReadOnlyList<WorkflowRunResult> GetForWorkflow(Guid workflowId)
    {
        if (!_runs.TryGetValue(workflowId, out var list))
        {
            return [];
        }

        lock (list)
        {
            return list.ToList();
        }
    }
}
