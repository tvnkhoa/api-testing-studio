using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="IWorkspaceSettingRepository"/> over the open workspace's <c>Settings</c> table.
/// A short-lived context is created per operation from the session's connection string, mirroring
/// <see cref="PackageMetadataRepository"/>.
/// </summary>
public sealed class WorkspaceSettingRepository : IWorkspaceSettingRepository
{
    private readonly WorkspaceSession _session;

    public WorkspaceSettingRepository(WorkspaceSession session)
    {
        _session = session;
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        await using var context = CreateContext();
        var setting = await context.Settings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken)
            .ConfigureAwait(false);
        return setting?.Value;
    }

    public async Task SetAsync(string key, string? value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        await using var context = CreateContext();

        var existing = await context.Settings
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            var workspaceId = _session.Current?.Id ?? Guid.Empty;
            await context.Settings
                .AddAsync(new WorkspaceSetting { WorkspaceId = workspaceId, Key = key, Value = value }, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            context.Entry(existing).CurrentValues.SetValues(existing with { Value = value });
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private WorkspaceDbContext CreateContext()
    {
        if (_session.ConnectionString is not { } connectionString)
        {
            throw new InvalidOperationException("Cannot access workspace settings: no workspace is open.");
        }

        return WorkspaceContextFactory.Create(connectionString);
    }
}
