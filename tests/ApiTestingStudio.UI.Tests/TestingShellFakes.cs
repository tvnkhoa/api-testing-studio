using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Dashboard;
using ApiTestingStudio.Application.Runs;
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

/// <summary>Empty <see cref="IDashboardService"/> for shell tests.</summary>
internal sealed class FakeDashboardService : IDashboardService
{
    public Task<Result<DashboardSnapshot>> GetSnapshotAsync(DashboardQuery query, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success(DashboardSnapshot.Empty));
}

/// <summary>No-op <see cref="IRunRecorder"/> for shell/editor tests.</summary>
internal sealed class FakeRunRecorder : IRunRecorder
{
    public Task RecordRequestAsync(Guid endpointId, HttpRequestModel request, HttpExecutionResult execution, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RecordWorkflowAsync(Workflow workflow, WorkflowRunResult result, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RecordStressAsync(StressRun stressRun, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

/// <summary>Empty <see cref="IRunStore"/> for shell tests.</summary>
internal sealed class FakeRunStore : IRunStore
{
    public Task SaveAsync(Run run, IReadOnlyList<RunStep> steps, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<Run>> ListAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Run>>([]);

    public Task<Run?> GetAsync(Guid runId, CancellationToken cancellationToken = default)
        => Task.FromResult<Run?>(null);

    public Task<IReadOnlyList<RunStep>> GetStepsAsync(Guid runId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<RunStep>>([]);
}

/// <summary>No-op <see cref="IRunReplayService"/> for shell tests.</summary>
internal sealed class FakeRunReplayService : IRunReplayService
{
    public Task<Result<RunReplayResult>> ReplayAsync(Guid runId, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Failure<RunReplayResult>(new Error("run.none", "No run.")));
}

/// <summary>Empty <see cref="ILogEventStore"/> for shell tests.</summary>
internal sealed class FakeLogEventStore : ILogEventStore
{
    public Task AppendAsync(IReadOnlyList<LogEventRecord> events, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<LogEventRecord>> QueryAsync(Guid workspaceId, LogEventQuery query, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<LogEventRecord>>([]);

    public Task<IReadOnlyList<string>> GetSourcesAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>([]);
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
