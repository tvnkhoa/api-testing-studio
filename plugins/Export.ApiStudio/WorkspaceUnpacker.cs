using System.IO.Compression;
using ApiTestingStudio.Plugin.Abstractions.Storage;

namespace ApiTestingStudio.Export.ApiStudio;

/// <summary>
/// Reads an <c>.apistudio</c> package into a staging directory and returns its contents. Pure and
/// offline. The caller owns the staging directory's lifetime. See ADR-0012.
/// </summary>
internal static class WorkspaceUnpacker
{
    public static async Task<WorkspacePackageContents> UnpackAsync(
        string packagePath,
        string stagingDirectory,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(packagePath))
        {
            throw new FileNotFoundException("Package file was not found.", packagePath);
        }

        Directory.CreateDirectory(stagingDirectory);
        var attachmentsDirectory = Path.Combine(stagingDirectory, "attachments");
        var databasePath = Path.Combine(stagingDirectory, WorkspacePackager.DatabaseEntryName);

        using var archive = ZipFile.OpenRead(packagePath);

        var manifestEntry = archive.GetEntry(WorkspacePackager.ManifestEntryName)
            ?? throw new InvalidDataException("Package is missing manifest.json.");
        PackageManifest manifest;
        await using (var manifestStream = manifestEntry.Open())
        {
            manifest = PackageManifestSerializer.Deserialize(manifestStream);
        }

        var dbEntry = archive.GetEntry(WorkspacePackager.DatabaseEntryName)
            ?? throw new InvalidDataException("Package is missing database.sqlite.");
        await using (var dbEntryStream = dbEntry.Open())
        await using (var dbTarget = new FileStream(databasePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await dbEntryStream.CopyToAsync(dbTarget, cancellationToken).ConfigureAwait(false);
        }

        var hasAttachments = false;
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (AttachmentStore.TryExtract(entry, attachmentsDirectory) && entry.Length > 0)
            {
                hasAttachments = true;
            }
        }

        return new WorkspacePackageContents(
            manifest,
            databasePath,
            hasAttachments ? attachmentsDirectory : null);
    }

    public static async Task<PackageManifest> ReadManifestAsync(string packagePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(packagePath))
        {
            throw new FileNotFoundException("Package file was not found.", packagePath);
        }

        using var archive = ZipFile.OpenRead(packagePath);
        var manifestEntry = archive.GetEntry(WorkspacePackager.ManifestEntryName)
            ?? throw new InvalidDataException("Package is missing manifest.json.");

        await using var manifestStream = manifestEntry.Open();
        return PackageManifestSerializer.Deserialize(manifestStream);
    }
}
