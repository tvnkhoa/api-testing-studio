using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="IStressRunStore"/> over the open workspace's database. A short-lived context is
/// created per operation from the session's connection string, mirroring <see cref="WorkflowRepository"/>.
/// A run and its metric rows are saved in one transaction. Ordering by timestamp is done client-side
/// because SQLite cannot <c>ORDER BY</c> a <see cref="DateTimeOffset"/> column.
/// </summary>
public sealed class StressRunRepository : IStressRunStore
{
    private readonly WorkspaceSession _session;

    public StressRunRepository(WorkspaceSession session)
    {
        _session = session;
    }

    public async Task SaveAsync(StressRun run, IReadOnlyList<StressMetrics> metrics, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(metrics);

        await using var context = CreateContext();

        await context.StressRuns.AddAsync(run, cancellationToken).ConfigureAwait(false);

        var rows = metrics.Select(m => m with { StressRunId = run.Id }).ToList();
        await context.StressMetrics.AddRangeAsync(rows, cancellationToken).ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<StressRun>> ListAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();

        var runs = await context.StressRuns
            .AsNoTracking()
            .Where(r => r.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return runs.OrderByDescending(r => r.CompletedUtc).ToList();
    }

    public async Task<IReadOnlyList<StressMetrics>> GetMetricsAsync(Guid stressRunId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();

        return await context.StressMetrics
            .AsNoTracking()
            .Where(m => m.StressRunId == stressRunId)
            .OrderBy(m => m.SequenceIndex)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private WorkspaceDbContext CreateContext()
    {
        if (_session.ConnectionString is not { } connectionString)
        {
            throw new InvalidOperationException("Cannot access stress runs: no workspace is open.");
        }

        return WorkspaceContextFactory.Create(connectionString);
    }
}
