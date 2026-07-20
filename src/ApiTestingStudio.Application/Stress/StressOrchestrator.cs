using System.Diagnostics;
using System.Text.Json;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.ApiRunner;
using ApiTestingStudio.Application.Runs;
using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Plugin.Abstractions.Runners;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Stress;

/// <summary>
/// Default <see cref="IStressOrchestrator"/>. Builds the workload from the target using the raw
/// <see cref="IRequestExecutor"/> (not the history-recording <c>IRequestExecutionService</c>, which
/// would flood the history table under load) or the <see cref="IWorkflowEngine"/>, then hands it to the
/// first loaded <see cref="IStressRunner"/> and persists the outcome via <see cref="IStressRunStore"/>.
/// </summary>
public sealed class StressOrchestrator : IStressOrchestrator
{
    private readonly IStressRunner? _runner;
    private readonly IRequestExecutor _executor;
    private readonly IEndpointRepository _endpoints;
    private readonly IServiceRepository _services;
    private readonly IWorkflowRepository _workflows;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IStressRunStore _store;
    private readonly IRunRecorder _runRecorder;
    private readonly IWorkspaceSession _session;
    private readonly IClock _clock;

    public StressOrchestrator(
        IEnumerable<IStressRunner> runners,
        IRequestExecutor executor,
        IEndpointRepository endpoints,
        IServiceRepository services,
        IWorkflowRepository workflows,
        IWorkflowEngine workflowEngine,
        IStressRunStore store,
        IRunRecorder runRecorder,
        IWorkspaceSession session,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(runners);
        _runner = runners.FirstOrDefault();
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _workflows = workflows ?? throw new ArgumentNullException(nameof(workflows));
        _workflowEngine = workflowEngine ?? throw new ArgumentNullException(nameof(workflowEngine));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _runRecorder = runRecorder ?? throw new ArgumentNullException(nameof(runRecorder));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<Result<StressRun>> RunAsync(
        StressRunRequest request,
        IProgress<StressMetricsSnapshot>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_session.IsOpen || _session.Current is not { } workspace)
        {
            return Result.Failure<StressRun>(StressErrors.NoWorkspaceOpen);
        }

        if (_runner is null)
        {
            return Result.Failure<StressRun>(StressErrors.RunnerUnavailable);
        }

        var plan = await BuildWorkloadAsync(request, cancellationToken).ConfigureAwait(false);
        if (plan.IsFailure)
        {
            return Result.Failure<StressRun>(plan.Error);
        }

        var started = _clock.UtcNow;
        var result = await _runner
            .RunAsync(request.Config, plan.Value.Workload, progress, cancellationToken)
            .ConfigureAwait(false);
        var completed = _clock.UtcNow;

        var run = MapRun(workspace.Id, request, plan.Value.TargetName, result, started, completed);
        var metrics = MapMetrics(run.Id, result.Metrics);
        await _store.SaveAsync(run, [metrics], cancellationToken).ConfigureAwait(false);
        await _runRecorder.RecordStressAsync(run, cancellationToken).ConfigureAwait(false);

        return Result.Success(run);
    }

    private async Task<Result<WorkloadPlan>> BuildWorkloadAsync(StressRunRequest request, CancellationToken cancellationToken)
    {
        switch (request.TargetKind)
        {
            case StressTargetKind.Request:
                if (request.Request is not { } model)
                {
                    return Result.Failure<WorkloadPlan>(StressErrors.NoTarget);
                }

                return Result.Success(new WorkloadPlan(
                    HttpWorkload(model),
                    request.TargetName ?? $"{model.Method} {model.Url}"));

            case StressTargetKind.Endpoint:
                if (request.EndpointId is not { } endpointId)
                {
                    return Result.Failure<WorkloadPlan>(StressErrors.NoTarget);
                }

                var endpoint = await _endpoints.GetAsync(endpointId, cancellationToken).ConfigureAwait(false);
                if (endpoint is null)
                {
                    return Result.Failure<WorkloadPlan>(RequestExecutionErrors.EndpointNotFound(endpointId));
                }

                var service = await _services.GetAsync(endpoint.ServiceId, cancellationToken).ConfigureAwait(false);
                var endpointRequest = new HttpRequestModel
                {
                    Method = endpoint.Method,
                    Url = CombineUrl(service?.BaseUrl, endpoint.Path),
                    Headers = DeserializeHeaders(endpoint.DefaultHeaders),
                    BodyKind = BodyKind.Json,
                    Body = endpoint.DefaultBody,
                };

                return Result.Success(new WorkloadPlan(
                    HttpWorkload(endpointRequest),
                    request.TargetName ?? endpoint.Name));

            case StressTargetKind.Workflow:
                if (request.WorkflowId is not { } workflowId)
                {
                    return Result.Failure<WorkloadPlan>(StressErrors.NoTarget);
                }

                var workflow = await _workflows.GetAsync(workflowId, cancellationToken).ConfigureAwait(false);
                if (workflow is null)
                {
                    return Result.Failure<WorkloadPlan>(WorkflowErrors.NotFound(workflowId));
                }

                return Result.Success(new WorkloadPlan(
                    WorkflowWorkload(workflow),
                    request.TargetName ?? workflow.Name));

            default:
                return Result.Failure<WorkloadPlan>(StressErrors.NoTarget);
        }
    }

    /// <summary>Drives one HTTP request and measures its wall-clock latency.</summary>
    private Func<CancellationToken, Task<StressSample>> HttpWorkload(HttpRequestModel request)
        => async cancellationToken =>
        {
            var start = Stopwatch.GetTimestamp();
            var result = await _executor.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
            var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            if (result.IsFailure)
            {
                return new StressSample(elapsedMs, Success: false, StatusCode: null);
            }

            var status = result.Value.Response.StatusCode;
            return new StressSample(elapsedMs, Success: status is >= 100 and < 400, status);
        };

    /// <summary>Drives one full workflow run and reports its engine-measured duration.</summary>
    private Func<CancellationToken, Task<StressSample>> WorkflowWorkload(Workflow workflow)
        => async cancellationToken =>
        {
            var run = await _workflowEngine
                .RunAsync(workflow, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return new StressSample(
                run.Duration.TotalMilliseconds,
                Success: run.Status == RunStatus.Passed,
                StatusCode: null);
        };

    private static StressRun MapRun(
        Guid workspaceId,
        StressRunRequest request,
        string targetName,
        StressRunResult result,
        DateTimeOffset started,
        DateTimeOffset completed)
    {
        var config = result.Config;
        var metrics = result.Metrics;
        return new StressRun
        {
            WorkspaceId = workspaceId,
            TargetKind = request.TargetKind,
            TargetId = request.TargetKind switch
            {
                StressTargetKind.Endpoint => request.EndpointId,
                StressTargetKind.Workflow => request.WorkflowId,
                _ => null,
            },
            TargetName = targetName,
            Mode = config.Mode,
            VirtualUsers = config.VirtualUsers,
            Iterations = config.Iterations,
            DurationMs = config.Duration is { } duration ? (long)duration.TotalMilliseconds : null,
            WarmupIterations = config.WarmupIterations,
            Cancelled = result.Cancelled,
            StartedUtc = started,
            CompletedUtc = completed,
            RequestCount = metrics.Completed,
            RequestsPerSecond = metrics.RequestsPerSecond,
            P95Ms = metrics.P95Ms,
            ErrorRate = metrics.ErrorRate,
        };
    }

    private static StressMetrics MapMetrics(Guid runId, StressMetricsSnapshot snapshot)
        => new()
        {
            StressRunId = runId,
            SequenceIndex = -1,
            ElapsedMs = (long)snapshot.Elapsed.TotalMilliseconds,
            RequestCount = snapshot.Completed,
            FailureCount = snapshot.Failed,
            RequestsPerSecond = snapshot.RequestsPerSecond,
            MinMs = snapshot.MinMs,
            AverageMs = snapshot.AverageMs,
            MaxMs = snapshot.MaxMs,
            P50Ms = snapshot.P50Ms,
            P95Ms = snapshot.P95Ms,
            P99Ms = snapshot.P99Ms,
            ErrorRate = snapshot.ErrorRate,
        };

    private static string CombineUrl(string? baseUrl, string path)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return path;
        }

        if (string.IsNullOrEmpty(path))
        {
            return baseUrl;
        }

        return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

    private static List<HttpHeader> DeserializeHeaders(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<HttpHeader>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed record WorkloadPlan(Func<CancellationToken, Task<StressSample>> Workload, string TargetName);
}
