using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="IVariableRepository"/> over short-lived contexts built from the open workspace
/// session. Secret variables store their <c>Value</c> as ciphertext.
/// </summary>
public sealed class VariableRepository : IVariableRepository
{
    private readonly WorkspaceSession _session;

    public VariableRepository(WorkspaceSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    public async Task<IReadOnlyList<Variable>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        return await context.Variables
            .AsNoTracking()
            .Where(v => v.WorkspaceId == workspaceId)
            .OrderBy(v => v.Scope)
            .ThenBy(v => v.Key)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Variable>> GetByScopeAsync(Guid workspaceId, VariableScope scope, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        return await context.Variables
            .AsNoTracking()
            .Where(v => v.WorkspaceId == workspaceId && v.Scope == scope)
            .OrderBy(v => v.Key)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Variable>> GetByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        return await context.Variables
            .AsNoTracking()
            .Where(v => v.EnvironmentId == environmentId)
            .OrderBy(v => v.Key)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Variable?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        return await context.Variables
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Variable variable, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(variable);

        await using var context = CreateContext();
        await context.Variables.AddAsync(variable, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Variable variable, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(variable);

        await using var context = CreateContext();
        context.Variables.Update(variable);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var variable = await context.Variables
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (variable is not null)
        {
            context.Variables.Remove(variable);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private WorkspaceDbContext CreateContext()
    {
        if (_session.ConnectionString is not { } connectionString)
        {
            throw new InvalidOperationException("Cannot access variables: no workspace is open.");
        }

        return WorkspaceContextFactory.Create(connectionString);
    }
}
