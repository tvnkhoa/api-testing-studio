using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="IServiceRepository"/> operating on the open workspace's database. A short-lived
/// context is created per operation from the session's connection string, mirroring
/// <see cref="PackageMetadataRepository"/>.
/// </summary>
public sealed class ServiceRepository : IServiceRepository
{
    private readonly WorkspaceSession _session;

    public ServiceRepository(WorkspaceSession session)
    {
        _session = session;
    }

    public async Task<IReadOnlyList<Service>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        return await context.Services
            .AsNoTracking()
            .Where(s => s.WorkspaceId == workspaceId)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Service?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        return await context.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Service service, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);

        await using var context = CreateContext();
        await context.Services.AddAsync(service, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Service service, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);

        await using var context = CreateContext();
        context.Services.Update(service);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteCascadeAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();

        var endpoints = await context.Endpoints
            .Where(e => e.ServiceId == serviceId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        context.Endpoints.RemoveRange(endpoints);

        var folders = await context.EndpointFolders
            .Where(f => f.ServiceId == serviceId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        context.EndpointFolders.RemoveRange(folders);

        var service = await context.Services
            .FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken)
            .ConfigureAwait(false);
        if (service is not null)
        {
            context.Services.Remove(service);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private WorkspaceDbContext CreateContext()
    {
        if (_session.ConnectionString is not { } connectionString)
        {
            throw new InvalidOperationException("Cannot access services: no workspace is open.");
        }

        return WorkspaceContextFactory.Create(connectionString);
    }
}
