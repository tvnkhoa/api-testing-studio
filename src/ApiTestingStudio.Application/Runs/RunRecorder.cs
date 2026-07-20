using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Runs;

/// <summary>
/// Default <see cref="IRunRecorder"/>. Maps execution outputs via <see cref="RunMapper"/>, persists them
/// through <see cref="IRunStore"/>, and publishes the saved run to <see cref="IMetricsFeed"/>. Store
/// failures are swallowed so recording (best-effort telemetry) never breaks the user's primary action;
/// the Application layer stays free of a logging dependency, so the failure is intentionally silent.
/// </summary>
public sealed class RunRecorder : IRunRecorder
{
    private readonly IRunStore _store;
    private readonly IMetricsFeed _metrics;
    private readonly IWorkspaceSession _session;
    private readonly IClock _clock;

    public RunRecorder(
        IRunStore store,
        IMetricsFeed metrics,
        IWorkspaceSession session,
        IClock clock)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public Task RecordRequestAsync(Guid endpointId, HttpRequestModel request, HttpExecutionResult execution, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(execution);

        if (_session.Current is not { } workspace)
        {
            return Task.CompletedTask;
        }

        var (run, steps) = RunMapper.BuildRequestRun(workspace.Id, endpointId, request, execution, _clock.UtcNow);
        return SaveAsync(run, steps, cancellationToken);
    }

    public Task RecordWorkflowAsync(Workflow workflow, WorkflowRunResult result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(result);

        if (_session.Current is not { } workspace)
        {
            return Task.CompletedTask;
        }

        var (run, steps) = RunMapper.BuildWorkflowRun(workspace.Id, workflow, result, _clock.UtcNow);
        return SaveAsync(run, steps, cancellationToken);
    }

    public Task RecordStressAsync(StressRun stressRun, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stressRun);

        if (!_session.IsOpen)
        {
            return Task.CompletedTask;
        }

        var (run, steps) = RunMapper.BuildStressRun(stressRun);
        return SaveAsync(run, steps, cancellationToken);
    }

    private async Task SaveAsync(Run run, IReadOnlyList<RunStep> steps, CancellationToken cancellationToken)
    {
        try
        {
            await _store.SaveAsync(run, steps, cancellationToken).ConfigureAwait(false);
            _metrics.Publish(run);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Best-effort telemetry: a failed run recording must not fail the user's primary action.
        }
    }
}
