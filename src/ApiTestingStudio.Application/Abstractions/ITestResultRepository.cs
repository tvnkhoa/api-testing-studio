using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Persists <see cref="TestRunResult"/>s (one row per test-case execution) for the open workspace.
/// Results are historical records; ordering by timestamp is done client-side because SQLite cannot
/// <c>ORDER BY</c> a <see cref="DateTimeOffset"/> (see the RequestHistory note in DATABASE_GUIDELINES).
/// </summary>
public interface ITestResultRepository
{
    /// <summary>Records a completed test-case run.</summary>
    Task AddAsync(TestRunResult result, CancellationToken cancellationToken = default);

    /// <summary>Lists results for a single case, most recent first.</summary>
    Task<IReadOnlyList<TestRunResult>> ListByCaseAsync(Guid testCaseId, CancellationToken cancellationToken = default);

    /// <summary>Lists results for all cases in a suite, most recent first.</summary>
    Task<IReadOnlyList<TestRunResult>> ListBySuiteAsync(Guid suiteId, CancellationToken cancellationToken = default);
}
