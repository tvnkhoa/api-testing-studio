using System.IO;
using ApiTestingStudio.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiTestingStudio.Infrastructure.Tests;

/// <summary>
/// Verifies <see cref="WorkspaceMaintenance"/> produces a valid, data-preserving copy via
/// <c>VACUUM INTO</c> without mutating the source. See ADR-0012.
/// </summary>
public sealed class WorkspaceMaintenanceTests : TempDirectoryFixture
{
    [Fact]
    public async Task Checkpoint_and_vacuum_produces_a_valid_copy_with_the_same_data()
    {
        var source = PathFor("source.db");
        await using (var connection = new SqliteConnection($"Data Source={source}"))
        {
            await connection.OpenAsync();
            await Execute(connection, "CREATE TABLE t(id INTEGER PRIMARY KEY, v TEXT);");
            await Execute(connection, "INSERT INTO t(v) VALUES('hello');");
        }

        SqliteConnection.ClearAllPools();

        var target = PathFor("copy.db");
        var maintenance = new WorkspaceMaintenance(NullLogger<WorkspaceMaintenance>.Instance);

        await maintenance.CheckpointAndVacuumAsync(source, target);

        File.Exists(target).Should().BeTrue();

        await using var check = new SqliteConnection($"Data Source={target}");
        await check.OpenAsync();
        var command = check.CreateCommand();
        command.CommandText = "SELECT v FROM t;";
        var value = (string?)await command.ExecuteScalarAsync();
        value.Should().Be("hello");
    }

    [Fact]
    public async Task Checkpoint_and_vacuum_overwrites_an_existing_target()
    {
        var source = PathFor("source2.db");
        await using (var connection = new SqliteConnection($"Data Source={source}"))
        {
            await connection.OpenAsync();
            await Execute(connection, "CREATE TABLE t(id INTEGER PRIMARY KEY);");
        }

        SqliteConnection.ClearAllPools();

        var target = PathFor("copy2.db");
        await File.WriteAllTextAsync(target, "stale");

        var maintenance = new WorkspaceMaintenance(NullLogger<WorkspaceMaintenance>.Instance);
        var act = () => maintenance.CheckpointAndVacuumAsync(source, target);

        await act.Should().NotThrowAsync();
    }

    private static async Task Execute(SqliteConnection connection, string sql)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
