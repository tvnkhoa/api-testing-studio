using System.IO;
using Microsoft.Data.Sqlite;

namespace ApiTestingStudio.Infrastructure.Tests;

/// <summary>
/// Base for tests that touch real SQLite files. Gives each test an isolated temp directory and
/// clears the SQLite connection pool on teardown so Windows releases file handles before cleanup.
/// </summary>
public abstract class TempDirectoryFixture : IDisposable
{
    protected TempDirectoryFixture()
    {
        TempDir = Path.Combine(Path.GetTempPath(), "ats-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(TempDir);
    }

    protected string TempDir { get; }

    protected string PathFor(string fileName) => Path.Combine(TempDir, fileName);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        try
        {
            if (Directory.Exists(TempDir))
            {
                Directory.Delete(TempDir, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup; a leaked temp dir must not fail the test run.
        }

        GC.SuppressFinalize(this);
    }
}
