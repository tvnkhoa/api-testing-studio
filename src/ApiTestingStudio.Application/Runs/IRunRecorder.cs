using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Runs;

/// <summary>
/// Records completed executions into the unified run-log tree (<c>IRunStore</c>) and notifies the
/// live metrics stream (Sprint 13). Recording is best-effort telemetry: it never throws back into the
/// caller's primary action, and it is a no-op when no workspace is open.
/// </summary>
public interface IRunRecorder
{
    /// <summary>Records a single request send with its request/response snapshot (for Replay).</summary>
    Task RecordRequestAsync(Guid endpointId, HttpRequestModel request, HttpExecutionResult execution, CancellationToken cancellationToken = default);

    /// <summary>Records a workflow run, flattening its node tree into steps.</summary>
    Task RecordWorkflowAsync(Workflow workflow, WorkflowRunResult result, CancellationToken cancellationToken = default);

    /// <summary>Records a stress run as a run header plus a summary step.</summary>
    Task RecordStressAsync(StressRun stressRun, CancellationToken cancellationToken = default);
}
