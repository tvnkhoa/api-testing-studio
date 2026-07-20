using System.Globalization;
using System.IO;
using ApiTestingStudio.Application.Backup;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// Filesystem <see cref="IBackupStore"/> over <c>&lt;app-data&gt;/backups/&lt;workspaceId&gt;/</c>. The
/// only component that knows where backup archives physically live. Archive file names carry a UTC
/// timestamp so they sort chronologically and prune deterministically. See ADR-0012.
/// </summary>
public sealed class FileBackupStore : IBackupStore
{
    private const string Extension = ".apistudio";
    private const string TimestampFormat = "yyyyMMdd'T'HHmmss'Z'";

    private readonly string _root;

    public FileBackupStore(string backupsRootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupsRootDirectory);
        _root = backupsRootDirectory;
    }

    public string AllocateBackupPath(Guid workspaceId, DateTimeOffset timestampUtc)
    {
        var directory = WorkspaceFolder(workspaceId);
        Directory.CreateDirectory(directory);

        var stamp = timestampUtc.UtcDateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture);
        var path = Path.Combine(directory, stamp + Extension);

        var attempt = 1;
        while (File.Exists(path))
        {
            path = Path.Combine(directory, $"{stamp}-{attempt++}{Extension}");
        }

        return path;
    }

    public IReadOnlyList<string> ListBackupFiles(Guid workspaceId)
    {
        var directory = WorkspaceFolder(workspaceId);
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return Directory.EnumerateFiles(directory, "*" + Extension)
            .OrderByDescending(Path.GetFileName, StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<Guid> ListBackedUpWorkspaces()
    {
        if (!Directory.Exists(_root))
        {
            return [];
        }

        var ids = new List<Guid>();
        foreach (var directory in Directory.EnumerateDirectories(_root))
        {
            if (Guid.TryParseExact(Path.GetFileName(directory), "N", out var id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    public void Prune(Guid workspaceId, int retain)
    {
        if (retain < 0)
        {
            retain = 0;
        }

        foreach (var old in ListBackupFiles(workspaceId).Skip(retain))
        {
            try
            {
                File.Delete(old);
            }
            catch (IOException)
            {
                // Best-effort: a locked stale archive must not fail the backup.
            }
        }
    }

    private string WorkspaceFolder(Guid workspaceId) => Path.Combine(_root, workspaceId.ToString("N"));
}
