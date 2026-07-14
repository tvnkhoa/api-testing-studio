using System.IO;
using ApiTestingStudio.Application.Workspaces;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Plugin.Abstractions.Storage;
using ApiTestingStudio.Shared.Results;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// SQLite-backed <see cref="IStorageProvider"/>. A <c>location</c> is a file path. Each workspace
/// is a self-contained SQLite file; opening applies EF migrations so the schema self-provisions,
/// and file/lock/corruption problems are turned into typed <see cref="Result"/> failures rather
/// than exceptions. Because callers depend only on the abstraction, alternative providers can be
/// added later without touching business logic.
/// </summary>
public sealed class SqliteStorageProvider : IStorageProvider
{
    private const int SqliteBusy = 5;
    private const int SqliteLocked = 6;
    private const int SqliteCorrupt = 11;
    private const int SqliteNotADatabase = 26;

    private readonly WorkspaceSession _session;
    private readonly ILogger<SqliteStorageProvider> _logger;

    public SqliteStorageProvider(WorkspaceSession session, ILogger<SqliteStorageProvider> logger)
    {
        _session = session;
        _logger = logger;
    }

    public string ProviderName => "sqlite";

    public bool IsOpen => _session.IsOpen;

    public async Task<Result> CreateAsync(string location, Workspace metadata, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(location);
        ArgumentNullException.ThrowIfNull(metadata);

        if (File.Exists(location))
        {
            return Result.Failure(WorkspaceErrors.AlreadyExists(location));
        }

        var connectionString = BuildConnectionString(location);

        try
        {
            EnsureDirectory(location);

            await using var context = WorkspaceContextFactory.Create(connectionString);
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            await context.Workspaces.AddAsync(metadata, cancellationToken).ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SqliteException ex)
        {
            _logger.LogError(ex, "SQLite error creating workspace at {Location}.", location);
            SqliteConnection.ClearAllPools();
            return Result.Failure(MapSqliteError(ex, location, WorkspaceErrors.CreateFailed(location)));
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error creating workspace at {Location}.", location);
            SqliteConnection.ClearAllPools();
            return Result.Failure(WorkspaceErrors.CreateFailed(location));
        }

        _session.Open(location, connectionString, metadata);
        _logger.LogInformation("Created workspace '{Name}' at {Location}.", metadata.Name, location);
        return Result.Success();
    }

    public async Task<Result> OpenAsync(string location, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(location);

        if (!File.Exists(location))
        {
            return Result.Failure(WorkspaceErrors.NotFound(location));
        }

        var connectionString = BuildConnectionString(location);

        try
        {
            await using var context = WorkspaceContextFactory.Create(connectionString);
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

            var workspace = await context.Workspaces
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (workspace is null)
            {
                SqliteConnection.ClearAllPools();
                return Result.Failure(WorkspaceErrors.Corrupt(location));
            }

            var schemaCheck = SchemaVersionValidator.Validate(workspace.SchemaVersion);
            if (schemaCheck.IsFailure)
            {
                SqliteConnection.ClearAllPools();
                return schemaCheck;
            }

            _session.Open(location, connectionString, workspace);
            _logger.LogInformation("Opened workspace '{Name}' at {Location}.", workspace.Name, location);
            return Result.Success();
        }
        catch (SqliteException ex)
        {
            _logger.LogError(ex, "SQLite error opening workspace at {Location}.", location);
            SqliteConnection.ClearAllPools();
            return Result.Failure(MapSqliteError(ex, location, WorkspaceErrors.OpenFailed(location)));
        }
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (_session.IsOpen)
        {
            var name = _session.Current?.Name;
            _session.Close();
            // Release the pooled connection so the OS file handle is freed (Windows keeps the file
            // locked otherwise, blocking reopen/delete).
            SqliteConnection.ClearAllPools();
            _logger.LogInformation("Closed workspace '{Name}'.", name);
        }

        return Task.CompletedTask;
    }

    public Task<Result> DeleteAsync(string location, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(location);

        if (IsCurrent(location))
        {
            _session.Close();
        }

        // Ensure no pooled handle keeps the file locked before we remove it.
        SqliteConnection.ClearAllPools();

        if (!File.Exists(location))
        {
            return Task.FromResult(Result.Failure(WorkspaceErrors.NotFound(location)));
        }

        try
        {
            DeleteWorkspaceFiles(location);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to delete workspace at {Location}.", location);
            return Task.FromResult(Result.Failure(WorkspaceErrors.Locked(location)));
        }

        _logger.LogInformation("Deleted workspace at {Location}.", location);
        return Task.FromResult(Result.Success());
    }

    public Task<Workspace?> GetWorkspaceAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_session.Current);

    public async Task SaveWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        if (_session.ConnectionString is not { } connectionString)
        {
            throw new InvalidOperationException("Cannot save workspace metadata: no workspace is open.");
        }

        await using var context = WorkspaceContextFactory.Create(connectionString);
        context.Workspaces.Update(workspace);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _session.UpdateCurrent(workspace);
    }

    private bool IsCurrent(string location)
        => _session.Location is not null
           && string.Equals(_session.Location, location, StringComparison.OrdinalIgnoreCase);

    private static string BuildConnectionString(string location)
        => new SqliteConnectionStringBuilder { DataSource = location }.ToString();

    private static void EnsureDirectory(string location)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(location));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static void DeleteWorkspaceFiles(string location)
    {
        File.Delete(location);
        // SQLite WAL/SHM sidecars, if present.
        DeleteIfExists(location + "-wal");
        DeleteIfExists(location + "-shm");
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static Error MapSqliteError(SqliteException ex, string location, Error fallback)
        => ex.SqliteErrorCode switch
        {
            SqliteBusy or SqliteLocked => WorkspaceErrors.Locked(location),
            SqliteCorrupt or SqliteNotADatabase => WorkspaceErrors.Corrupt(location),
            _ => fallback,
        };
}
