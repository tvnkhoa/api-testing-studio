using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Stress;
using ApiTestingStudio.Application.Testing;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Plugin.Abstractions.Runners;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.UI.Tests;

/// <summary>No-op <see cref="ITestSuiteRepository"/> for shell tests.</summary>
internal sealed class FakeTestSuiteRepository : ITestSuiteRepository
{
    public Task<IReadOnlyList<TestSuite>> ListAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TestSuite>>([]);

    public Task<TestSuite?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<TestSuite?>(null);

    public Task AddAsync(TestSuite suite, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task UpdateAsync(TestSuite suite, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

/// <summary>No-op <see cref="ITestCaseRepository"/> for shell tests.</summary>
internal sealed class FakeTestCaseRepository : ITestCaseRepository
{
    public Task<TestCase?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<TestCase?>(null);

    public Task<IReadOnlyList<TestCaseDefinition>> ListBySuiteAsync(Guid suiteId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TestCaseDefinition>>([]);

    public Task<IReadOnlyList<TestCaseDefinition>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TestCaseDefinition>>([]);

    public Task SaveAsync(TestCase testCase, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

/// <summary>Scripted <see cref="ITestSuiteExecutor"/> for shell tests.</summary>
internal sealed class FakeTestSuiteExecutor : ITestSuiteExecutor
{
    public Task<Result<TestRunResult>> RunCaseAsync(Guid testCaseId, IProgress<TestRunResult>? progress = null, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Failure<TestRunResult>(new Error("test.none", "No result.")));

    public Task<Result<IReadOnlyList<TestRunResult>>> RunSuiteAsync(Guid suiteId, IProgress<TestRunResult>? progress = null, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success<IReadOnlyList<TestRunResult>>([]));
}

/// <summary>No-op <see cref="IStressOrchestrator"/> for shell tests.</summary>
internal sealed class FakeStressOrchestrator : IStressOrchestrator
{
    public Task<Result<StressRun>> RunAsync(StressRunRequest request, IProgress<StressMetricsSnapshot>? progress = null, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Failure<StressRun>(new Error("stress.none", "No run.")));
}

/// <summary>No-op <see cref="IWorkflowRepository"/> for shell tests.</summary>
internal sealed class FakeShellWorkflowRepository : IWorkflowRepository
{
    public Task<Workflow?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<Workflow?>(null);

    public Task<IReadOnlyList<WorkflowDefinition>> ListAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<WorkflowDefinition>>([]);

    public Task SaveAsync(Workflow workflow, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
