using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.ApiRunner;
using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Plugin.Abstractions.Assertions;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Tests;

/// <summary>Configurable <see cref="IAssertion"/> that records the last context it received.</summary>
internal sealed class FakeAssertion : IAssertion
{
    public FakeAssertion(string kind, AssertionOutcome outcome = AssertionOutcome.Passed)
    {
        Kind = kind;
        Outcome = outcome;
    }

    public string Kind { get; }

    public AssertionOutcome Outcome { get; set; }

    public AssertionContext? LastContext { get; private set; }

    public Task<AssertionEvaluation> EvaluateAsync(AssertionContext context, CancellationToken cancellationToken = default)
    {
        LastContext = context;
        return Task.FromResult(new AssertionEvaluation(Outcome, $"actual={context.Actual}"));
    }
}

internal sealed class InMemoryTestSuiteRepository : ITestSuiteRepository
{
    public List<TestSuite> Suites { get; } = [];

    public Task<IReadOnlyList<TestSuite>> ListAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TestSuite>>(Suites.Where(s => s.WorkspaceId == workspaceId).ToList());

    public Task<TestSuite?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Suites.FirstOrDefault(s => s.Id == id));

    public Task AddAsync(TestSuite suite, CancellationToken cancellationToken = default)
    {
        Suites.Add(suite);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TestSuite suite, CancellationToken cancellationToken = default)
    {
        var index = Suites.FindIndex(s => s.Id == suite.Id);
        if (index >= 0)
        {
            Suites[index] = suite;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Suites.RemoveAll(s => s.Id == id);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryTestCaseRepository : ITestCaseRepository
{
    public List<TestCaseDefinition> Cases { get; } = [];

    public List<AssertionDefinition> Assertions { get; } = [];

    public Task<TestCase?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var definition = Cases.FirstOrDefault(c => c.Id == id);
        if (definition is null)
        {
            return Task.FromResult<TestCase?>(null);
        }

        var assertions = Assertions.Where(a => a.TestCaseId == id).OrderBy(a => a.SortOrder).ToList();
        return Task.FromResult<TestCase?>(new TestCase { Definition = definition, Assertions = assertions });
    }

    public Task<IReadOnlyList<TestCaseDefinition>> ListBySuiteAsync(Guid suiteId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TestCaseDefinition>>(Cases.Where(c => c.TestSuiteId == suiteId).ToList());

    public Task<IReadOnlyList<TestCaseDefinition>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TestCaseDefinition>>(Cases.Where(c => c.WorkspaceId == workspaceId).ToList());

    public Task SaveAsync(TestCase testCase, CancellationToken cancellationToken = default)
    {
        var index = Cases.FindIndex(c => c.Id == testCase.Definition.Id);
        if (index >= 0)
        {
            Cases[index] = testCase.Definition;
        }
        else
        {
            Cases.Add(testCase.Definition);
        }

        Assertions.RemoveAll(a => a.TestCaseId == testCase.Definition.Id);
        Assertions.AddRange(testCase.Assertions.Select(a => a with { TestCaseId = testCase.Definition.Id }));
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Assertions.RemoveAll(a => a.TestCaseId == id);
        Cases.RemoveAll(c => c.Id == id);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryTestResultRepository : ITestResultRepository
{
    public List<TestRunResult> Results { get; } = [];

    public Task AddAsync(TestRunResult result, CancellationToken cancellationToken = default)
    {
        Results.Add(result);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TestRunResult>> ListByCaseAsync(Guid testCaseId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TestRunResult>>(
            Results.Where(r => r.TestCaseId == testCaseId).OrderByDescending(r => r.TimestampUtc).ToList());

    public Task<IReadOnlyList<TestRunResult>> ListBySuiteAsync(Guid suiteId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TestRunResult>>(
            Results.Where(r => r.TestSuiteId == suiteId).OrderByDescending(r => r.TimestampUtc).ToList());
}

/// <summary>Configurable <see cref="IRequestExecutionService"/> — records the call, returns a scripted result.</summary>
internal sealed class FakeRequestExecutionService : IRequestExecutionService
{
    public FakeRequestExecutionService()
    {
        ResultToReturn = Result.Success(new HttpExecutionResult
        {
            Response = new HttpResponseModel { StatusCode = 200, ReasonPhrase = "OK", Body = "{\"ok\":true}" },
            Timing = new RequestTiming { Total = TimeSpan.FromMilliseconds(15) },
        });
    }

    public Result<HttpExecutionResult> ResultToReturn { get; set; }

    public int CallCount { get; private set; }

    public Guid? LastEndpointId { get; private set; }

    public HttpRequestModel? LastRequest { get; private set; }

    public Task<Result<HttpExecutionResult>> SendAsync(Guid endpointId, HttpRequestModel request, Guid? profileId = null, CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastEndpointId = endpointId;
        LastRequest = request;
        return Task.FromResult(ResultToReturn);
    }
}

/// <summary>Configurable <see cref="IWorkflowEngine"/> — returns a scripted run result.</summary>
internal sealed class FakeWorkflowEngine : IWorkflowEngine
{
    public WorkflowRunResult ResultToReturn { get; set; } = new() { Status = RunStatus.Passed };

    public Task<WorkflowRunResult> RunAsync(
        Workflow workflow,
        WorkflowRunOptions? options = null,
        IWorkflowContext? context = null,
        IProgress<NodeRunResult>? progress = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(ResultToReturn);
}

internal sealed class InMemoryWorkflowRepository : IWorkflowRepository
{
    public List<Workflow> Workflows { get; } = [];

    public Task<Workflow?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Workflows.FirstOrDefault(w => w.Id == id));

    public Task<IReadOnlyList<WorkflowDefinition>> ListAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<WorkflowDefinition>>([]);

    public Task SaveAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        Workflows.Add(workflow);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Workflows.RemoveAll(w => w.Id == id);
        return Task.CompletedTask;
    }
}
