using System.IO;
using ApiTestingStudio.Export.ApiStudio;
using ApiTestingStudio.Infrastructure.Persistence;
using ApiTestingStudio.Plugin.Abstractions.Storage;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiTestingStudio.PluginHost.Tests;

/// <summary>
/// Full packaging pipeline over a REAL SQLite database: checkpoint + VACUUM INTO a snapshot, pack it
/// (with attachments + manifest) into an <c>.apistudio</c> ZIP via the real serializer, unpack it, and
/// assert the workspace data and attachments survive the round-trip. Covers the Sprint 14 acceptance
/// criterion "export a workspace and re-import it with data intact." See ADR-0012.
/// </summary>
public sealed class ApiStudioEndToEndTests : IDisposable
{
    private readonly string _root;

    public ApiStudioEndToEndTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ats-e2e", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public async Task Export_then_import_preserves_database_rows_and_attachments()
    {
        // Arrange: a real workspace database with data, plus a sidecar attachment.
        var workspaceDb = Path.Combine(_root, "workspace.atsdb");
        await using (var connection = new SqliteConnection($"Data Source={workspaceDb}"))
        {
            await connection.OpenAsync();
            await Execute(connection, "CREATE TABLE endpoints(id INTEGER PRIMARY KEY, name TEXT);");
            await Execute(connection, "INSERT INTO endpoints(name) VALUES('GetUsers'),('CreateUser');");
        }

        SqliteConnection.ClearAllPools();

        var attachmentsDir = Path.Combine(_root, "workspace.attachments");
        Directory.CreateDirectory(attachmentsDir);
        await File.WriteAllTextAsync(Path.Combine(attachmentsDir, "spec.txt"), "openapi");

        var maintenance = new WorkspaceMaintenance(NullLogger<WorkspaceMaintenance>.Instance);
        var serializer = new ApiStudioPackageSerializer();

        // Act: maintenance snapshot → pack → unpack.
        var snapshot = Path.Combine(_root, "snapshot.sqlite");
        await maintenance.CheckpointAndVacuumAsync(workspaceDb, snapshot);

        var packagePath = Path.Combine(_root, "workspace.apistudio");
        var manifest = new PackageManifest(
            PackageManifest.CurrentFormatVersion, 9, "1.0.0", Guid.NewGuid(), "Demo",
            DateTimeOffset.UnixEpoch, [], new SecretBinding(true, "fp"));
        await serializer.SaveAsync(new WorkspacePackageRequest(snapshot, attachmentsDir, packagePath, manifest));

        var staging = Path.Combine(_root, "staging");
        var contents = await serializer.LoadAsync(packagePath, staging);

        // Assert: the restored database opens and still has both rows; the attachment survived.
        await using var restored = new SqliteConnection($"Data Source={contents.DatabasePath}");
        await restored.OpenAsync();
        var command = restored.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM endpoints;";
        var count = (long)(await command.ExecuteScalarAsync())!;
        count.Should().Be(2);

        contents.AttachmentsDirectory.Should().NotBeNull();
        File.ReadAllText(Path.Combine(contents.AttachmentsDirectory!, "spec.txt")).Should().Be("openapi");
    }

    private static async Task Execute(SqliteConnection connection, string sql)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch (IOException)
        {
            // best-effort
        }
    }
}
