using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="IRunStore"/> over the open workspace's database. A short-lived context is
/// created per operation from the session's connection string, mirroring <see cref="StressRunRepository"/>.
/// A run and its steps are saved in one transaction. Ordering by <see cref="Run.CompletedUtc"/> is done
/// client-side because SQLite cannot <c>ORDER BY</c> a <see cref="DateTimeOffset"/> column.
/// </summary>
public sealed class RunStore : IRunStore
{
    private readonly WorkspaceSession _session;

    public RunStore(WorkspaceSession session)
    {
        _session = session;
    }

    public async Task SaveAsync(Run run, IReadOnlyList<RunStep> steps, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(steps);

        await using var context = CreateContext();

        await context.Runs.AddAsync(run, cancellationToken).ConfigureAwait(false);

        var rows = steps.Select(s => s with { RunId = run.Id }).ToList();
        await context.RunSteps.AddRangeAsync(rows, cancellationToken).ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Run>> ListAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();

        var runs = await context.Runs
            .AsNoTracking()
            .Where(r => r.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return runs
            .OrderByDescending(r => r.CompletedUtc ?? r.StartedUtc)
            .ToList();
    }

    public async Task<Run?> GetAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();

        return await context.Runs
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RunStep>> GetStepsAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();

        return await context.RunSteps
            .AsNoTracking()
            .Where(s => s.RunId == runId)
            .OrderBy(s => s.Order)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private WorkspaceDbContext CreateContext()
    {
        if (_session.ConnectionString is not { } connectionString)
        {
            throw new InvalidOperationException("Cannot access runs: no workspace is open.");
        }

        return WorkspaceContextFactory.Create(connectionString);
    }
}
