using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="ILogEventStore"/> over the open workspace's database. Appends come from the
/// buffered Serilog sink; queries drive the Log Viewer. Level and source filters run in SQL; recency
/// ordering and the free-text filter run client-side (SQLite cannot <c>ORDER BY</c> a
/// <see cref="DateTimeOffset"/>, and <c>Contains</c> is case-insensitive only in memory).
/// </summary>
public sealed class LogEventStore : ILogEventStore
{
    private readonly WorkspaceSession _session;

    public LogEventStore(WorkspaceSession session)
    {
        _session = session;
    }

    public async Task AppendAsync(IReadOnlyList<LogEventRecord> events, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);
        if (events.Count == 0)
        {
            return;
        }

        await using var context = CreateContext();
        await context.LogEvents.AddRangeAsync(events, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<LogEventRecord>> QueryAsync(Guid workspaceId, LogEventQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await using var context = CreateContext();

        var q = context.LogEvents
            .AsNoTracking()
            .Where(e => e.WorkspaceId == workspaceId);

        if (query.Levels is { Count: > 0 } levels)
        {
            q = q.Where(e => levels.Contains(e.Level));
        }

        if (!string.IsNullOrEmpty(query.Source))
        {
            q = q.Where(e => e.Source == query.Source);
        }

        var rows = await q.ToListAsync(cancellationToken).ConfigureAwait(false);

        IEnumerable<LogEventRecord> filtered = rows;
        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            filtered = filtered.Where(e =>
                e.Message.Contains(query.SearchText, StringComparison.OrdinalIgnoreCase));
        }

        return filtered
            .OrderByDescending(e => e.TimestampUtc)
            .Take(Math.Max(1, query.Limit))
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetSourcesAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();

        var sources = await context.LogEvents
            .AsNoTracking()
            .Where(e => e.WorkspaceId == workspaceId)
            .Select(e => e.Source)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        sources.Sort(StringComparer.OrdinalIgnoreCase);
        return sources;
    }

    private WorkspaceDbContext CreateContext()
    {
        if (_session.ConnectionString is not { } connectionString)
        {
            throw new InvalidOperationException("Cannot access log events: no workspace is open.");
        }

        return WorkspaceContextFactory.Create(connectionString);
    }
}
