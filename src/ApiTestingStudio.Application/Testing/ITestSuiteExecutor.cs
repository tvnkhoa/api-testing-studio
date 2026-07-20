using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Testing;

/// <summary>
/// Executes test cases and suites: runs each case's target (an endpoint request or a workflow),
/// evaluates its assertions via <see cref="IAssertionRunner"/>, persists a <see cref="TestRunResult"/>,
/// and reports aggregate pass/fail. Pure orchestration; holds no HTTP or persistence types.
/// </summary>
public interface ITestSuiteExecutor
{
    /// <summary>
    /// Runs a single case and records its result. Returns a failure only when the case cannot be
    /// loaded; an execution or assertion failure is reported as a <see cref="TestRunResult"/> with a
    /// <c>Failed</c> status. When <paramref name="progress"/> is supplied it receives the result.
    /// </summary>
    Task<Result<TestRunResult>> RunCaseAsync(
        Guid testCaseId,
        IProgress<TestRunResult>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs every case in a suite in order and records their results, reporting each via
    /// <paramref name="progress"/> as it completes. Returns a failure only when the suite cannot be
    /// loaded.
    /// </summary>
    Task<Result<IReadOnlyList<TestRunResult>>> RunSuiteAsync(
        Guid suiteId,
        IProgress<TestRunResult>? progress = null,
        CancellationToken cancellationToken = default);
}
