using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="IProfileRepository"/> over short-lived contexts built from the open workspace
/// session. Secret material is already ciphertext on the <see cref="ProfileDefinition"/> by the time
/// it reaches this layer.
/// </summary>
public sealed class ProfileRepository : IProfileRepository
{
    private readonly WorkspaceSession _session;

    public ProfileRepository(WorkspaceSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    public async Task<IReadOnlyList<ProfileDefinition>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        return await context.Profiles
            .AsNoTracking()
            .Where(p => p.WorkspaceId == workspaceId)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ProfileDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        return await context.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(ProfileDefinition profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        await using var context = CreateContext();
        await context.Profiles.AddAsync(profile, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(ProfileDefinition profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        await using var context = CreateContext();
        context.Profiles.Update(profile);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var profile = await context.Profiles
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (profile is not null)
        {
            context.Profiles.Remove(profile);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private WorkspaceDbContext CreateContext()
    {
        if (_session.ConnectionString is not { } connectionString)
        {
            throw new InvalidOperationException("Cannot access profiles: no workspace is open.");
        }

        return WorkspaceContextFactory.Create(connectionString);
    }
}
