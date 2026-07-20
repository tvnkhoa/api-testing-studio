using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="ITestSuiteRepository"/> over short-lived contexts built from the open
/// workspace session, mirroring <see cref="ProfileRepository"/>.
/// </summary>
public sealed class TestSuiteRepository : ITestSuiteRepository
{
    private readonly WorkspaceSession _session;

    public TestSuiteRepository(WorkspaceSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    public async Task<IReadOnlyList<TestSuite>> ListAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        return await context.TestSuites
            .AsNoTracking()
            .Where(s => s.WorkspaceId == workspaceId)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TestSuite?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        return await context.TestSuites
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(TestSuite suite, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(suite);

        await using var context = CreateContext();
        await context.TestSuites.AddAsync(suite, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(TestSuite suite, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(suite);

        await using var context = CreateContext();
        context.TestSuites.Update(suite);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();

        // Detach member cases (move to ungrouped) rather than deleting them with the suite.
        var cases = await context.TestCases
            .Where(c => c.TestSuiteId == id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var testCase in cases)
        {
            context.TestCases.Update(testCase with { TestSuiteId = null });
        }

        var suite = await context.TestSuites
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (suite is not null)
        {
            context.TestSuites.Remove(suite);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private WorkspaceDbContext CreateContext()
    {
        if (_session.ConnectionString is not { } connectionString)
        {
            throw new InvalidOperationException("Cannot access test suites: no workspace is open.");
        }

        return WorkspaceContextFactory.Create(connectionString);
    }
}
