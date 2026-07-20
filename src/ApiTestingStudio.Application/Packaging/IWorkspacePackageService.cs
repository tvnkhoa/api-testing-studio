using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Packaging;

/// <summary>
/// Orchestrates <c>.apistudio</c> export and import over the workspace lifecycle. Export packages the
/// currently open workspace (DB maintenance → build manifest → serialize). Import validates the
/// package manifest, installs the database + attachments at a target location, and opens it. See
/// ADR-0012 and <c>.claude/FEATURES/Packaging.md</c>.
/// </summary>
public interface IWorkspacePackageService
{
    /// <summary>
    /// Exports the currently open workspace to an <c>.apistudio</c> package at
    /// <paramref name="targetPackagePath"/>. Fails with <c>workspace.none_open</c> when none is open.
    /// </summary>
    Task<Result<PackageExportResult>> ExportAsync(
        string targetPackagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports the package at <paramref name="packagePath"/>, installing its workspace at
    /// <paramref name="targetLocation"/> and opening it. Fails when the manifest is unreadable, the
    /// workspace schema is newer than supported, or the package format major version is unsupported.
    /// </summary>
    Task<Result<PackageImportResult>> ImportAsync(
        string packagePath,
        string targetLocation,
        CancellationToken cancellationToken = default);
}
