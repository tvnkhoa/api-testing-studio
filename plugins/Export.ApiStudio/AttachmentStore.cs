using System.IO.Compression;

namespace ApiTestingStudio.Export.ApiStudio;

/// <summary>
/// Byte-level attachment handling for the package: copies a workspace's attachment files into the
/// ZIP under <c>attachments/</c> and extracts them back out. Path <em>convention</em> (where the
/// sidecar folder lives relative to the workspace) is resolved by the Application orchestrator; this
/// type only moves bytes for whatever directory it is given. See ADR-0012.
/// </summary>
internal static class AttachmentStore
{
    public const string EntryPrefix = "attachments/";

    /// <summary>Writes every file under <paramref name="attachmentsDirectory"/> into the archive.</summary>
    public static void PackInto(ZipArchive archive, string attachmentsDirectory)
    {
        if (!Directory.Exists(attachmentsDirectory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(attachmentsDirectory, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(attachmentsDirectory, file).Replace('\\', '/');
            var entry = archive.CreateEntry(EntryPrefix + relative, CompressionLevel.Optimal);

            using var entryStream = entry.Open();
            using var source = File.OpenRead(file);
            source.CopyTo(entryStream);
        }
    }

    /// <summary>
    /// Extracts an <c>attachments/</c> entry to <paramref name="targetDirectory"/>. Returns false for
    /// entries that are not attachments. Guards against path traversal (Zip Slip).
    /// </summary>
    public static bool TryExtract(ZipArchiveEntry entry, string targetDirectory)
    {
        if (!entry.FullName.StartsWith(EntryPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var relative = entry.FullName[EntryPrefix.Length..];
        if (relative.Length == 0)
        {
            return true; // the directory entry itself
        }

        var destination = Path.GetFullPath(Path.Combine(targetDirectory, relative));
        var root = Path.GetFullPath(targetDirectory) + Path.DirectorySeparatorChar;
        if (!destination.StartsWith(root, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Package attachment '{entry.FullName}' escapes the target directory.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        entry.ExtractToFile(destination, overwrite: true);
        return true;
    }
}
