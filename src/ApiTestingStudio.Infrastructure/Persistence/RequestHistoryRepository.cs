using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="IRequestHistoryRepository"/> operating on the open workspace's database. A
/// short-lived context is created per operation from the session's connection string, mirroring
/// <see cref="ServiceRepository"/>.
/// </summary>
public sealed class RequestHistoryRepository : IRequestHistoryRepository
{
    private readonly WorkspaceSession _session;

    public RequestHistoryRepository(WorkspaceSession session)
    {
        _session = session;
    }

    public async Task<IReadOnlyList<RequestHistoryEntry>> GetByEndpointAsync(Guid endpointId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        // SQLite cannot ORDER BY a DateTimeOffset column, so order client-side after materializing.
        var entries = await context.RequestHistory
            .AsNoTracking()
            .Where(e => e.EndpointId == endpointId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return entries
            .OrderByDescending(e => e.TimestampUtc)
            .ToList();
    }

    public async Task<RequestHistoryEntry?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        return await context.RequestHistory
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(RequestHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await using var context = CreateContext();
        await context.RequestHistory.AddAsync(entry, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteByEndpointAsync(Guid endpointId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var entries = await context.RequestHistory
            .Where(e => e.EndpointId == endpointId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        context.RequestHistory.RemoveRange(entries);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private WorkspaceDbContext CreateContext()
    {
        if (_session.ConnectionString is not { } connectionString)
        {
            throw new InvalidOperationException("Cannot access request history: no workspace is open.");
        }

        return WorkspaceContextFactory.Create(connectionString);
    }
}
