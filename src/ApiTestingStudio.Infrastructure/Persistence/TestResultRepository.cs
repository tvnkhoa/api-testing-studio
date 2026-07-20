using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="ITestResultRepository"/> over the open workspace's database. Timestamp
/// ordering is applied client-side because SQLite cannot <c>ORDER BY</c> a
/// <see cref="DateTimeOffset"/> column, mirroring <see cref="RequestHistoryRepository"/>.
/// </summary>
public sealed class TestResultRepository : ITestResultRepository
{
    private readonly WorkspaceSession _session;

    public TestResultRepository(WorkspaceSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    public async Task AddAsync(TestRunResult result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        await using var context = CreateContext();
        await context.TestResults.AddAsync(result, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TestRunResult>> ListByCaseAsync(Guid testCaseId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var results = await context.TestResults
            .AsNoTracking()
            .Where(r => r.TestCaseId == testCaseId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return OrderNewestFirst(results);
    }

    public async Task<IReadOnlyList<TestRunResult>> ListBySuiteAsync(Guid suiteId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var results = await context.TestResults
            .AsNoTracking()
            .Where(r => r.TestSuiteId == suiteId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return OrderNewestFirst(results);
    }

    private static List<TestRunResult> OrderNewestFirst(List<TestRunResult> results)
        => results.OrderByDescending(r => r.TimestampUtc).ToList();

    private WorkspaceDbContext CreateContext()
    {
        if (_session.ConnectionString is not { } connectionString)
        {
            throw new InvalidOperationException("Cannot access test results: no workspace is open.");
        }

        return WorkspaceContextFactory.Create(connectionString);
    }
}
