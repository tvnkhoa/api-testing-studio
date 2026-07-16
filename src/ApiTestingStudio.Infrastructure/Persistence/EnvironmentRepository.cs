using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="IEnvironmentRepository"/> over short-lived contexts built from the open
/// workspace session.
/// </summary>
public sealed class EnvironmentRepository : IEnvironmentRepository
{
    private readonly WorkspaceSession _session;

    public EnvironmentRepository(WorkspaceSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    public async Task<IReadOnlyList<EnvironmentDefinition>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        return await context.Environments
            .AsNoTracking()
            .Where(e => e.WorkspaceId == workspaceId)
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<EnvironmentDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        return await context.Environments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(EnvironmentDefinition environment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(environment);

        await using var context = CreateContext();
        await context.Environments.AddAsync(environment, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(EnvironmentDefinition environment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(environment);

        await using var context = CreateContext();
        context.Environments.Update(environment);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteCascadeAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();

        var variables = await context.Variables
            .Where(v => v.EnvironmentId == environmentId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        context.Variables.RemoveRange(variables);

        var environment = await context.Environments
            .FirstOrDefaultAsync(e => e.Id == environmentId, cancellationToken)
            .ConfigureAwait(false);
        if (environment is not null)
        {
            context.Environments.Remove(environment);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private WorkspaceDbContext CreateContext()
    {
        if (_session.ConnectionString is not { } connectionString)
        {
            throw new InvalidOperationException("Cannot access environments: no workspace is open.");
        }

        return WorkspaceContextFactory.Create(connectionString);
    }
}
