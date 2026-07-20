using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="ITestCaseRepository"/> over the open workspace's database. A case is stored
/// across the <c>TestCases</c> + <c>Assertions</c> tables and hydrated into a runtime
/// <see cref="TestCase"/> aggregate; saves replace the child assertion rows wholesale, mirroring
/// <see cref="WorkflowRepository"/>.
/// </summary>
public sealed class TestCaseRepository : ITestCaseRepository
{
    private readonly WorkspaceSession _session;

    public TestCaseRepository(WorkspaceSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    public async Task<TestCase?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();

        var definition = await context.TestCases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (definition is null)
        {
            return null;
        }

        var assertions = await context.Assertions
            .AsNoTracking()
            .Where(a => a.TestCaseId == id)
            .OrderBy(a => a.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new TestCase { Definition = definition, Assertions = assertions };
    }

    public async Task<IReadOnlyList<TestCaseDefinition>> ListBySuiteAsync(Guid suiteId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        return await context.TestCases
            .AsNoTracking()
            .Where(c => c.TestSuiteId == suiteId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TestCaseDefinition>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        return await context.TestCases
            .AsNoTracking()
            .Where(c => c.WorkspaceId == workspaceId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SaveAsync(TestCase testCase, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(testCase);

        await using var context = CreateContext();
        var definition = testCase.Definition;

        var exists = await context.TestCases
            .AsNoTracking()
            .AnyAsync(c => c.Id == definition.Id, cancellationToken)
            .ConfigureAwait(false);
        if (exists)
        {
            context.TestCases.Update(definition);

            // Replace the child assertions wholesale so removed rows do not linger.
            var oldAssertions = await context.Assertions
                .Where(a => a.TestCaseId == definition.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            context.Assertions.RemoveRange(oldAssertions);
        }
        else
        {
            await context.TestCases.AddAsync(definition, cancellationToken).ConfigureAwait(false);
        }

        var assertions = testCase.Assertions.Select(a => a with { TestCaseId = definition.Id }).ToList();
        await context.Assertions.AddRangeAsync(assertions, cancellationToken).ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();

        var assertions = await context.Assertions
            .Where(a => a.TestCaseId == id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        context.Assertions.RemoveRange(assertions);

        var definition = await context.TestCases
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (definition is not null)
        {
            context.TestCases.Remove(definition);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private WorkspaceDbContext CreateContext()
    {
        if (_session.ConnectionString is not { } connectionString)
        {
            throw new InvalidOperationException("Cannot access test cases: no workspace is open.");
        }

        return WorkspaceContextFactory.Create(connectionString);
    }
}
