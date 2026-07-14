using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="IEndpointFolderRepository"/> operating on the open workspace's database. A
/// short-lived context is created per operation from the session's connection string, mirroring
/// <see cref="PackageMetadataRepository"/>.
/// </summary>
public sealed class EndpointFolderRepository : IEndpointFolderRepository
{
    private readonly WorkspaceSession _session;

    public EndpointFolderRepository(WorkspaceSession session)
    {
        _session = session;
    }

    public async Task<IReadOnlyList<EndpointFolder>> GetByServiceAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        return await context.EndpointFolders
            .AsNoTracking()
            .Where(f => f.ServiceId == serviceId)
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<EndpointFolder?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        return await context.EndpointFolders
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(EndpointFolder folder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(folder);

        await using var context = CreateContext();
        await context.EndpointFolders.AddAsync(folder, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(EndpointFolder folder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(folder);

        await using var context = CreateContext();
        context.EndpointFolders.Update(folder);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteCascadeAsync(Guid folderId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();

        var folder = await context.EndpointFolders
            .FirstOrDefaultAsync(f => f.Id == folderId, cancellationToken)
            .ConfigureAwait(false);
        if (folder is null)
        {
            return;
        }

        // Load every folder of the owning service so the descendant subtree can be resolved in memory.
        var serviceFolders = await context.EndpointFolders
            .Where(f => f.ServiceId == folder.ServiceId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var subtreeIds = ResolveSubtree(folderId, serviceFolders);

        var endpoints = await context.Endpoints
            .Where(e => e.FolderId != null && subtreeIds.Contains(e.FolderId.Value))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        context.Endpoints.RemoveRange(endpoints);

        context.EndpointFolders.RemoveRange(serviceFolders.Where(f => subtreeIds.Contains(f.Id)));

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Returns <paramref name="rootId"/> plus all folders nested beneath it (breadth-first).</summary>
    private static HashSet<Guid> ResolveSubtree(Guid rootId, IReadOnlyList<EndpointFolder> allFolders)
    {
        var byParent = allFolders
            .Where(f => f.ParentFolderId is not null)
            .GroupBy(f => f.ParentFolderId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new HashSet<Guid> { rootId };
        var queue = new Queue<Guid>();
        queue.Enqueue(rootId);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!byParent.TryGetValue(current, out var children))
            {
                continue;
            }

            foreach (var child in children)
            {
                if (result.Add(child.Id))
                {
                    queue.Enqueue(child.Id);
                }
            }
        }

        return result;
    }

    private WorkspaceDbContext CreateContext()
    {
        if (_session.ConnectionString is not { } connectionString)
        {
            throw new InvalidOperationException("Cannot access endpoint folders: no workspace is open.");
        }

        return WorkspaceContextFactory.Create(connectionString);
    }
}
