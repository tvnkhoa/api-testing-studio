using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.ApiRunner;
using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Runs;

/// <summary>
/// Default <see cref="IRunReplayService"/>. Loads a run from <see cref="IRunStore"/> and re-executes it:
/// request runs re-drive their captured request snapshots through <see cref="IRequestExecutor"/>;
/// workflow runs reload the workflow and re-run it via <see cref="IWorkflowEngine"/>.
/// </summary>
public sealed class RunReplayService : IRunReplayService
{
    private readonly IRunStore _runStore;
    private readonly IRequestExecutor _executor;
    private readonly IWorkflowRepository _workflows;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkspaceSession _session;

    public RunReplayService(
        IRunStore runStore,
        IRequestExecutor executor,
        IWorkflowRepository workflows,
        IWorkflowEngine workflowEngine,
        IWorkspaceSession session)
    {
        _runStore = runStore ?? throw new ArgumentNullException(nameof(runStore));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _workflows = workflows ?? throw new ArgumentNullException(nameof(workflows));
        _workflowEngine = workflowEngine ?? throw new ArgumentNullException(nameof(workflowEngine));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public async Task<Result<RunReplayResult>> ReplayAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        if (!_session.IsOpen)
        {
            return Result.Failure<RunReplayResult>(RunErrors.NoWorkspaceOpen);
        }

        var run = await _runStore.GetAsync(runId, cancellationToken).ConfigureAwait(false);
        if (run is null)
        {
            return Result.Failure<RunReplayResult>(RunErrors.NotFound(runId));
        }

        return run.Source switch
        {
            RunSource.Request => await ReplayRequestAsync(runId, cancellationToken).ConfigureAwait(false),
            RunSource.Workflow => await ReplayWorkflowAsync(run, cancellationToken).ConfigureAwait(false),
            _ => Result.Failure<RunReplayResult>(RunErrors.StressReplayUnsupported),
        };
    }

    private async Task<Result<RunReplayResult>> ReplayRequestAsync(Guid runId, CancellationToken cancellationToken)
    {
        var steps = await _runStore.GetStepsAsync(runId, cancellationToken).ConfigureAwait(false);
        var requests = steps
            .Where(s => !string.IsNullOrEmpty(s.RequestSnapshot))
            .Select(s => RequestSnapshotSerializer.DeserializeRequest(s.RequestSnapshot!))
            .OfType<HttpRequestModel>()
            .ToList();

        if (requests.Count == 0)
        {
            return Result.Failure<RunReplayResult>(RunErrors.NotReplayable);
        }

        var succeeded = 0;
        foreach (var request in requests)
        {
            var result = await _executor.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
            if (result.IsSuccess && result.Value.Response.StatusCode is >= 100 and < 400)
            {
                succeeded++;
            }
        }

        var status = succeeded == requests.Count ? RunStatus.Passed : RunStatus.Failed;
        return Result.Success(new RunReplayResult
        {
            Status = status,
            StepCount = requests.Count,
            Summary = $"Replayed {requests.Count} request(s); {succeeded} succeeded.",
        });
    }

    private async Task<Result<RunReplayResult>> ReplayWorkflowAsync(Run run, CancellationToken cancellationToken)
    {
        if (run.WorkflowId is not { } workflowId)
        {
            return Result.Failure<RunReplayResult>(RunErrors.NotReplayable);
        }

        var workflow = await _workflows.GetAsync(workflowId, cancellationToken).ConfigureAwait(false);
        if (workflow is null)
        {
            return Result.Failure<RunReplayResult>(WorkflowErrors.NotFound(workflowId));
        }

        var result = await _workflowEngine.RunAsync(workflow, cancellationToken: cancellationToken).ConfigureAwait(false);
        return Result.Success(new RunReplayResult
        {
            Status = result.Status,
            StepCount = result.Nodes.Count,
            Summary = $"Replayed workflow '{workflow.Name}' — {result.Status} ({result.Nodes.Count} node(s)).",
        });
    }
}
