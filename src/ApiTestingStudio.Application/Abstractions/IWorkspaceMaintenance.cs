namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// SQLite maintenance used before packaging a workspace. Produces a clean, WAL-free, compacted copy
/// of a workspace database WITHOUT mutating the live file. Implemented in Infrastructure
/// (<c>PRAGMA wal_checkpoint(TRUNCATE)</c> then <c>VACUUM INTO</c>). See ADR-0012.
/// </summary>
public interface IWorkspaceMaintenance
{
    /// <summary>
    /// Checkpoints the WAL of the database at <paramref name="sourceDatabasePath"/> and writes a
    /// compacted copy to <paramref name="targetDatabasePath"/> (which must not already exist).
    /// </summary>
    Task CheckpointAndVacuumAsync(
        string sourceDatabasePath,
        string targetDatabasePath,
        CancellationToken cancellationToken = default);
}
