using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="IEndpointRepository"/> operating on the open workspace's database. A short-lived
/// context is created per operation from the session's connection string, mirroring
/// <see cref="PackageMetadataRepository"/>.
/// </summary>
public sealed class EndpointRepository : IEndpointRepository
{
    private readonly WorkspaceSession _session;

    public EndpointRepository(WorkspaceSession session)
    {
        _session = session;
    }

    public async Task<IReadOnlyList<Endpoint>> GetByServiceAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        return await context.Endpoints
            .AsNoTracking()
            .Where(e => e.ServiceId == serviceId)
            .OrderBy(e => e.SortOrder)
            .ThenBy(e => e.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Endpoint?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        return await context.Endpoints
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Endpoint endpoint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        await using var context = CreateContext();
        await context.Endpoints.AddAsync(endpoint, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Endpoint endpoint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        await using var context = CreateContext();
        context.Endpoints.Update(endpoint);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();

        var existing = await context.Endpoints
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            context.Endpoints.Remove(existing);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private WorkspaceDbContext CreateContext()
    {
        if (_session.ConnectionString is not { } connectionString)
        {
            throw new InvalidOperationException("Cannot access endpoints: no workspace is open.");
        }

        return WorkspaceContextFactory.Create(connectionString);
    }
}
