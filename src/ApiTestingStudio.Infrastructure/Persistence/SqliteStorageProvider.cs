using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Plugin.Abstractions.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// SQLite-backed <see cref="IStorageProvider"/>. This is the Phase 1 provider; because callers
/// depend only on the abstraction, alternative providers (SQL Server, PostgreSQL, cloud) can be
/// added later without touching business logic.
/// </summary>
public sealed class SqliteStorageProvider : IStorageProvider
{
    private readonly WorkspaceDbContext _dbContext;
    private readonly ILogger<SqliteStorageProvider> _logger;

    public SqliteStorageProvider(WorkspaceDbContext dbContext, ILogger<SqliteStorageProvider> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public string ProviderName => "sqlite";

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Ensuring SQLite workspace database schema is up to date.");
        await _dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Workspace?> GetWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => await _dbContext.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken)
            .ConfigureAwait(false);

    public async Task SaveWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var exists = await _dbContext.Workspaces
            .AsNoTracking()
            .AnyAsync(w => w.Id == workspace.Id, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            _dbContext.Workspaces.Update(workspace);
        }
        else
        {
            await _dbContext.Workspaces.AddAsync(workspace, cancellationToken).ConfigureAwait(false);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
