using System.IO;
using ApiTestingStudio.Application.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// SQLite <see cref="IWorkspaceMaintenance"/>. Produces a clean, WAL-free, compacted copy of a
/// workspace database for packaging without mutating the live file: a WAL checkpoint folds any
/// pending frames, then <c>VACUUM INTO</c> writes a defragmented single-file snapshot to the target.
/// Uses a non-pooled connection so no handle to the live file lingers afterwards. See ADR-0012.
/// </summary>
public sealed class WorkspaceMaintenance : IWorkspaceMaintenance
{
    private readonly ILogger<WorkspaceMaintenance> _logger;

    public WorkspaceMaintenance(ILogger<WorkspaceMaintenance> logger)
    {
        _logger = logger;
    }

    public async Task CheckpointAndVacuumAsync(
        string sourceDatabasePath,
        string targetDatabasePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDatabasePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDatabasePath);

        if (!File.Exists(sourceDatabasePath))
        {
            throw new FileNotFoundException("Source workspace database was not found.", sourceDatabasePath);
        }

        if (File.Exists(targetDatabasePath))
        {
            File.Delete(targetDatabasePath);
        }

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = sourceDatabasePath,
            Pooling = false,
        }.ToString();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await using var checkpoint = connection.CreateCommand();
            checkpoint.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
            await checkpoint.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SqliteException ex)
        {
            // A busy checkpoint is non-fatal: VACUUM INTO still captures a consistent snapshot that
            // includes committed WAL frames. Log and continue.
            _logger.LogDebug(ex, "WAL checkpoint before packaging was skipped (database busy).");
        }

        await using (var vacuum = connection.CreateCommand())
        {
            vacuum.CommandText = "VACUUM main INTO $target;";
            vacuum.Parameters.AddWithValue("$target", targetDatabasePath);
            await vacuum.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
