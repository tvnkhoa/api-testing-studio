using System.Text.Json;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.ApiRunner;
using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Testing;

/// <summary>
/// Default <see cref="ITestSuiteExecutor"/>. An endpoint case is executed via the shared
/// <see cref="IRequestExecutionService"/> (reused from Sprint 06); a workflow case via the
/// <see cref="IWorkflowEngine"/> (Sprint 08), with assertions evaluated against the last API node's
/// published outputs. Both paths funnel through the one <see cref="IAssertionRunner"/> for a
/// consistent assertion context.
/// </summary>
public sealed class TestSuiteExecutor : ITestSuiteExecutor
{
    private readonly ITestCaseRepository _cases;
    private readonly ITestSuiteRepository _suites;
    private readonly ITestResultRepository _results;
    private readonly IEndpointRepository _endpoints;
    private readonly IServiceRepository _services;
    private readonly IRequestExecutionService _requestExecution;
    private readonly IWorkflowRepository _workflows;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IAssertionRunner _assertionRunner;
    private readonly IClock _clock;

    public TestSuiteExecutor(
        ITestCaseRepository cases,
        ITestSuiteRepository suites,
        ITestResultRepository results,
        IEndpointRepository endpoints,
        IServiceRepository services,
        IRequestExecutionService requestExecution,
        IWorkflowRepository workflows,
        IWorkflowEngine workflowEngine,
        IAssertionRunner assertionRunner,
        IClock clock)
    {
        _cases = cases ?? throw new ArgumentNullException(nameof(cases));
        _suites = suites ?? throw new ArgumentNullException(nameof(suites));
        _results = results ?? throw new ArgumentNullException(nameof(results));
        _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _requestExecution = requestExecution ?? throw new ArgumentNullException(nameof(requestExecution));
        _workflows = workflows ?? throw new ArgumentNullException(nameof(workflows));
        _workflowEngine = workflowEngine ?? throw new ArgumentNullException(nameof(workflowEngine));
        _assertionRunner = assertionRunner ?? throw new ArgumentNullException(nameof(assertionRunner));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<Result<TestRunResult>> RunCaseAsync(
        Guid testCaseId,
        IProgress<TestRunResult>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var testCase = await _cases.GetAsync(testCaseId, cancellationToken).ConfigureAwait(false);
        if (testCase is null)
        {
            return Result.Failure<TestRunResult>(TestingErrors.CaseNotFound(testCaseId));
        }

        var result = await ExecuteCaseAsync(testCase, cancellationToken).ConfigureAwait(false);
        await _results.AddAsync(result, cancellationToken).ConfigureAwait(false);
        progress?.Report(result);
        return Result.Success(result);
    }

    public async Task<Result<IReadOnlyList<TestRunResult>>> RunSuiteAsync(
        Guid suiteId,
        IProgress<TestRunResult>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var suite = await _suites.GetAsync(suiteId, cancellationToken).ConfigureAwait(false);
        if (suite is null)
        {
            return Result.Failure<IReadOnlyList<TestRunResult>>(TestingErrors.SuiteNotFound(suiteId));
        }

        var cases = await _cases.ListBySuiteAsync(suiteId, cancellationToken).ConfigureAwait(false);
        var results = new List<TestRunResult>(cases.Count);
        foreach (var definition in cases)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var runResult = await RunCaseAsync(definition.Id, progress, cancellationToken).ConfigureAwait(false);
            if (runResult.IsSuccess)
            {
                results.Add(runResult.Value);
            }
        }

        return Result.Success<IReadOnlyList<TestRunResult>>(results);
    }

    private async Task<TestRunResult> ExecuteCaseAsync(TestCase testCase, CancellationToken cancellationToken)
    {
        var definition = testCase.Definition;
        var started = _clock.UtcNow;

        var execution = await ResolveExecutionAsync(definition, cancellationToken).ConfigureAwait(false);

        List<AssertionResult> assertionResults;
        if (execution.IsFailure)
        {
            assertionResults =
            [
                new AssertionResult
                {
                    AssertionId = Guid.Empty,
                    Kind = "execution",
                    Outcome = AssertionOutcome.Failed,
                    Message = execution.Error.Message,
                },
            ];
        }
        else
        {
            var evaluated = await _assertionRunner
                .EvaluateAsync(execution.Value, testCase.Assertions, cancellationToken)
                .ConfigureAwait(false);
            assertionResults = [.. evaluated];
        }

        var completed = _clock.UtcNow;
        var passed = assertionResults.Count(a => a.Outcome == AssertionOutcome.Passed);
        var failed = assertionResults.Count(a => a.Outcome == AssertionOutcome.Failed);
        var skipped = assertionResults.Count(a => a.Outcome == AssertionOutcome.Skipped);

        return new TestRunResult
        {
            WorkspaceId = definition.WorkspaceId,
            TestCaseId = definition.Id,
            TestSuiteId = definition.TestSuiteId,
            Status = failed > 0 ? RunStatus.Failed : RunStatus.Passed,
            PassedCount = passed,
            FailedCount = failed,
            SkippedCount = skipped,
            DurationMs = (long)(completed - started).TotalMilliseconds,
            TimestampUtc = started,
            DetailsJson = TestResultDetails.Serialize(assertionResults),
        };
    }

    private async Task<Result<HttpExecutionResult>> ResolveExecutionAsync(TestCaseDefinition definition, CancellationToken cancellationToken)
    {
        if (definition.EndpointId is { } endpointId)
        {
            return await ExecuteEndpointAsync(endpointId, cancellationToken).ConfigureAwait(false);
        }

        if (definition.WorkflowId is { } workflowId)
        {
            return await ExecuteWorkflowAsync(workflowId, cancellationToken).ConfigureAwait(false);
        }

        return Result.Failure<HttpExecutionResult>(
            new Error("test.no_target", "The test case has neither an endpoint nor a workflow to run."));
    }

    private async Task<Result<HttpExecutionResult>> ExecuteEndpointAsync(Guid endpointId, CancellationToken cancellationToken)
    {
        var endpoint = await _endpoints.GetAsync(endpointId, cancellationToken).ConfigureAwait(false);
        if (endpoint is null)
        {
            return Result.Failure<HttpExecutionResult>(RequestExecutionErrors.EndpointNotFound(endpointId));
        }

        var service = await _services.GetAsync(endpoint.ServiceId, cancellationToken).ConfigureAwait(false);
        var request = new HttpRequestModel
        {
            Method = endpoint.Method,
            Url = CombineUrl(service?.BaseUrl, endpoint.Path),
            Headers = DeserializeHeaders(endpoint.DefaultHeaders),
            BodyKind = BodyKind.Json,
            Body = endpoint.DefaultBody,
        };

        return await _requestExecution.SendAsync(endpointId, request, profileId: null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<HttpExecutionResult>> ExecuteWorkflowAsync(Guid workflowId, CancellationToken cancellationToken)
    {
        var workflow = await _workflows.GetAsync(workflowId, cancellationToken).ConfigureAwait(false);
        if (workflow is null)
        {
            return Result.Failure<HttpExecutionResult>(WorkflowErrors.NotFound(workflowId));
        }

        var run = await _workflowEngine.RunAsync(workflow, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (run.Status == RunStatus.Failed)
        {
            return Result.Failure<HttpExecutionResult>(
                WorkflowErrors.NodeFailed(workflow.Name, run.Error ?? "the workflow run did not pass."));
        }

        return Result.Success(SynthesizeResponse(run));
    }

    /// <summary>
    /// Builds an <see cref="HttpExecutionResult"/> from the last API node's published
    /// <c>status</c>/<c>reason</c>/<c>body</c> outputs so case-level assertions on a workflow evaluate
    /// against the same context shape as an endpoint case.
    /// </summary>
    private static HttpExecutionResult SynthesizeResponse(WorkflowRunResult run)
    {
        var apiNode = run.Nodes.LastOrDefault(n => n.Kind == WorkflowNodeKind.Api && n.Outputs.Count > 0);
        var outputs = apiNode?.Outputs ?? new Dictionary<string, string>();
        return SyntheticHttpResponse.FromNodeOutputs(outputs, run.Duration);
    }

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
}
