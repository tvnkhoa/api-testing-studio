using ApiTestingStudio.Application.ApiRunner;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.Runs;

/// <summary>
/// Pure mapping from execution outputs (request send / workflow run / stress run) into the unified
/// <see cref="Run"/> + <see cref="RunStep"/> tree persisted by <c>IRunStore</c>. Kept free of I/O so it
/// is trivially unit-testable; the recorder stamps ids/timestamps and does the persistence.
/// </summary>
internal static class RunMapper
{
    public static (Run Run, IReadOnlyList<RunStep> Steps) BuildRequestRun(
        Guid workspaceId,
        Guid endpointId,
        HttpRequestModel request,
        HttpExecutionResult execution,
        DateTimeOffset completedUtc)
    {
        var durationMs = (long)execution.Timing.Total.TotalMilliseconds;
        var status = execution.Response.StatusCode is >= 100 and < 400 ? RunStatus.Passed : RunStatus.Failed;
        var label = $"{request.Method} {request.Url}";

        var run = new Run
        {
            WorkspaceId = workspaceId,
            Source = RunSource.Request,
            TargetId = endpointId,
            TargetName = label,
            Status = status,
            StartedUtc = completedUtc - execution.Timing.Total,
            CompletedUtc = completedUtc,
            DurationMs = durationMs,
        };

        var step = new RunStep
        {
            RunId = run.Id,
            Order = 0,
            Name = label,
            Kind = "Request",
            Status = status,
            StatusCode = execution.Response.StatusCode,
            DurationMs = durationMs,
            StartedUtc = run.StartedUtc,
            RequestSnapshot = RequestSnapshotSerializer.Serialize(request),
            ResponseSnapshot = RequestSnapshotSerializer.Serialize(execution.Response),
        };

        return (run, [step]);
    }

    public static (Run Run, IReadOnlyList<RunStep> Steps) BuildWorkflowRun(
        Guid workspaceId,
        Workflow workflow,
        WorkflowRunResult result,
        DateTimeOffset completedUtc)
    {
        var run = new Run
        {
            WorkspaceId = workspaceId,
            Source = RunSource.Workflow,
            WorkflowId = workflow.Id,
            TargetId = workflow.Id,
            TargetName = workflow.Name,
            Status = result.Status,
            StartedUtc = completedUtc - result.Duration,
            CompletedUtc = completedUtc,
            DurationMs = (long)result.Duration.TotalMilliseconds,
            Error = result.Error,
        };

        var steps = new List<RunStep>();
        Flatten(run.Id, parentStepId: null, result.Nodes, steps);
        return (run, steps);
    }

    public static (Run Run, IReadOnlyList<RunStep> Steps) BuildStressRun(StressRun stress)
    {
        var durationMs = (long)(stress.CompletedUtc - stress.StartedUtc).TotalMilliseconds;
        var status = stress.Cancelled
            ? RunStatus.Cancelled
            : stress.ErrorRate > 0 ? RunStatus.Failed : RunStatus.Passed;

        var run = new Run
        {
            WorkspaceId = stress.WorkspaceId,
            Source = RunSource.Stress,
            WorkflowId = stress.TargetKind == StressTargetKind.Workflow ? stress.TargetId : null,
            TargetId = stress.TargetId,
            TargetName = stress.TargetName,
            Status = status,
            StartedUtc = stress.StartedUtc,
            CompletedUtc = stress.CompletedUtc,
            DurationMs = durationMs,
        };

        var step = new RunStep
        {
            RunId = run.Id,
            Order = 0,
            Name = $"{stress.Mode} · {stress.RequestCount} requests · {stress.RequestsPerSecond:F1} rps · P95 {stress.P95Ms:F0} ms",
            Kind = "Stress",
            Status = status,
            DurationMs = durationMs,
            StartedUtc = stress.StartedUtc,
        };

        return (run, [step]);
    }

    private static void Flatten(Guid runId, Guid? parentStepId, IReadOnlyList<NodeRunResult> nodes, List<RunStep> sink)
    {
        var order = 0;
        foreach (var node in nodes)
        {
            var step = new RunStep
            {
                RunId = runId,
                ParentStepId = parentStepId,
                Order = order++,
                Name = node.NodeName,
                Kind = node.Kind.ToString(),
                Status = node.Status,
                DurationMs = (long)node.Duration.TotalMilliseconds,
                Iteration = node.Iteration,
                Error = node.Error,
            };
            sink.Add(step);

            if (node.Children.Count > 0)
            {
                Flatten(runId, step.Id, node.Children, sink);
            }
        }
    }
}
