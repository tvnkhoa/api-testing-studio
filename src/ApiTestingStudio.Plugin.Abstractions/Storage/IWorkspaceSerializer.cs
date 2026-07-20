namespace ApiTestingStudio.Plugin.Abstractions.Storage;

/// <summary>Explicit inputs for packaging a workspace. The serializer copies these verbatim — it does
/// no database work and does not read the open session, keeping it pure and testable.</summary>
public sealed record WorkspacePackageRequest(
    string SourceDatabasePath,
    string? AttachmentsDirectory,
    string TargetPackagePath,
    PackageManifest Manifest);

/// <summary>What a package unpacks to: the parsed manifest plus the extracted <c>database.sqlite</c>
/// and <c>attachments/</c> laid out under the caller-supplied staging directory.</summary>
public sealed record WorkspacePackageContents(
    PackageManifest Manifest,
    string DatabasePath,
    string? AttachmentsDirectory);

/// <summary>
/// Reads and writes the portable <c>.apistudio</c> package
/// (manifest.json + database.sqlite + attachments/). Implemented by <c>Export.ApiStudio</c> as pure,
/// offline byte I/O; orchestration (DB maintenance, manifest assembly, install/open) lives in the
/// Application layer's <c>IWorkspacePackageService</c>. See ADR-0012.
/// </summary>
public interface IWorkspaceSerializer
{
    /// <summary>The package format handled (e.g. "apistudio").</summary>
    string Format { get; }

    /// <summary>Writes the package described by <paramref name="request"/> to its target path.</summary>
    Task SaveAsync(WorkspacePackageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads only the <c>manifest.json</c> from a package without extracting the database or
    /// attachments — cheap enough to call while listing many packages (e.g. backups).
    /// </summary>
    Task<PackageManifest> ReadManifestAsync(string packagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts a package into <paramref name="stagingDirectory"/> and returns its contents. The
    /// caller owns the staging directory's lifetime (and cleanup).
    /// </summary>
    Task<WorkspacePackageContents> LoadAsync(
        string packagePath,
        string stagingDirectory,
        CancellationToken cancellationToken = default);
}
