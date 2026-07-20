using System.IO.Compression;
using ApiTestingStudio.Plugin.Abstractions.Storage;

namespace ApiTestingStudio.Export.ApiStudio;

/// <summary>
/// Writes an <c>.apistudio</c> package: ZIP(manifest.json + database.sqlite + attachments/). Pure and
/// offline; consumes only the explicit paths in the request. See ADR-0012.
/// </summary>
internal static class WorkspacePackager
{
    internal const string ManifestEntryName = "manifest.json";
    internal const string DatabaseEntryName = "database.sqlite";

    public static async Task PackAsync(WorkspacePackageRequest request, CancellationToken cancellationToken)
    {
        if (!File.Exists(request.SourceDatabasePath))
        {
            throw new FileNotFoundException("Source database for packaging was not found.", request.SourceDatabasePath);
        }

        var targetDirectory = Path.GetDirectoryName(Path.GetFullPath(request.TargetPackagePath));
        if (!string.IsNullOrEmpty(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        // Write to a temp file first, then move into place, so a failure never leaves a half-written
        // package at the target path.
        var tempPackage = request.TargetPackagePath + ".tmp";
        if (File.Exists(tempPackage))
        {
            File.Delete(tempPackage);
        }

        try
        {
            await using (var packageStream = new FileStream(tempPackage, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var archive = new ZipArchive(packageStream, ZipArchiveMode.Create))
            {
                var manifestEntry = archive.CreateEntry(ManifestEntryName, CompressionLevel.Optimal);
                await using (var manifestStream = manifestEntry.Open())
                {
                    var bytes = PackageManifestSerializer.Serialize(request.Manifest);
                    await manifestStream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                }

                var dbEntry = archive.CreateEntry(DatabaseEntryName, CompressionLevel.Optimal);
                await using (var dbEntryStream = dbEntry.Open())
                await using (var dbSource = new FileStream(request.SourceDatabasePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await dbSource.CopyToAsync(dbEntryStream, cancellationToken).ConfigureAwait(false);
                }

                if (request.AttachmentsDirectory is { } attachments)
                {
                    AttachmentStore.PackInto(archive, attachments);
                }
            }

            if (File.Exists(request.TargetPackagePath))
            {
                File.Delete(request.TargetPackagePath);
            }

            File.Move(tempPackage, request.TargetPackagePath);
        }
        finally
        {
            if (File.Exists(tempPackage))
            {
                File.Delete(tempPackage);
            }
        }
    }
}
