using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="IPackageMetadataRepository"/> operating on the open workspace's database.
/// A short-lived context is created per operation from the session's connection string.
/// </summary>
public sealed class PackageMetadataRepository : IPackageMetadataRepository
{
    private readonly WorkspaceSession _session;

    public PackageMetadataRepository(WorkspaceSession session)
    {
        _session = session;
    }

    public async Task<IReadOnlyList<PackageMetadata>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        return await context.Packages
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpsertAsync(PackageMetadata package, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);

        await using var context = CreateContext();

        var existing = await context.Packages
            .FirstOrDefaultAsync(p => p.PluginId == package.PluginId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            await context.Packages.AddAsync(package, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Keep the existing row's identity (PluginId is unique), overwrite the other columns.
            context.Entry(existing).CurrentValues.SetValues(package with { Id = existing.Id });
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);

        await using var context = CreateContext();

        var existing = await context.Packages
            .FirstOrDefaultAsync(p => p.PluginId == pluginId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            context.Packages.Remove(existing);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private WorkspaceDbContext CreateContext()
    {
        if (_session.ConnectionString is not { } connectionString)
        {
            throw new InvalidOperationException("Cannot access package metadata: no workspace is open.");
        }

        return WorkspaceContextFactory.Create(connectionString);
    }
}
