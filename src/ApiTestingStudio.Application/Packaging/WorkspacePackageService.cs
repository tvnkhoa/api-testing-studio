using System.IO;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Workspaces;
using ApiTestingStudio.Plugin.Abstractions.Storage;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Packaging;

/// <summary>
/// Default <see cref="IWorkspacePackageService"/>. Orchestrates <c>.apistudio</c> export/import over
/// the workspace lifecycle: export runs DB maintenance, assembles the <see cref="PackageManifest"/>,
/// and writes via the plugin serializer; import validates the manifest, installs the database +
/// attachments, and opens the workspace. Holds no packaging bytes itself. See ADR-0012.
/// </summary>
public sealed class WorkspacePackageService : IWorkspacePackageService
{
    private static readonly string AppVersion =
        typeof(WorkspacePackageService).Assembly.GetName().Version?.ToString() ?? "0.0.0";

    private readonly IWorkspaceSession _session;
    private readonly IWorkspaceService _workspaceService;
    private readonly IWorkspaceMaintenance _maintenance;
    private readonly IPackageMetadataRepository _packages;
    private readonly IKeyStore _keyStore;
    private readonly IInstalledPluginCatalog _installedPlugins;
    private readonly IClock _clock;
    private readonly IWorkspaceSerializer? _serializer;

    public WorkspacePackageService(
        IWorkspaceSession session,
        IWorkspaceService workspaceService,
        IWorkspaceMaintenance maintenance,
        IPackageMetadataRepository packages,
        IKeyStore keyStore,
        IInstalledPluginCatalog installedPlugins,
        IClock clock,
        IEnumerable<IWorkspaceSerializer> serializers)
    {
        _session = session;
        _workspaceService = workspaceService;
        _maintenance = maintenance;
        _packages = packages;
        _keyStore = keyStore;
        _installedPlugins = installedPlugins;
        _clock = clock;
        _serializer = serializers?.FirstOrDefault(s => s.Format == "apistudio");
    }

    public async Task<Result<PackageExportResult>> ExportAsync(
        string targetPackagePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPackagePath);

        if (_session is not { IsOpen: true, Location: { } location, Current: { } workspace })
        {
            return Result.Failure<PackageExportResult>(WorkspaceErrors.NoneOpen);
        }

        if (_serializer is null)
        {
            return Result.Failure<PackageExportResult>(PackageErrors.NoSerializer);
        }

        var work = CreateTempDirectory();
        try
        {
            var tempDb = Path.Combine(work, "database.sqlite");
            await _maintenance.CheckpointAndVacuumAsync(location, tempDb, cancellationToken).ConfigureAwait(false);

            var manifest = await BuildManifestAsync(workspace, cancellationToken).ConfigureAwait(false);

            var attachments = WorkspaceAttachmentPaths.ForWorkspace(location);
            var attachmentsDir = Directory.Exists(attachments) ? attachments : null;

            await _serializer
                .SaveAsync(new WorkspacePackageRequest(tempDb, attachmentsDir, targetPackagePath, manifest), cancellationToken)
                .ConfigureAwait(false);

            var size = new FileInfo(targetPackagePath).Length;
            return Result.Success(new PackageExportResult(targetPackagePath, size));
        }
        finally
        {
            TryDeleteDirectory(work);
        }
    }

    public async Task<Result<PackageImportResult>> ImportAsync(
        string packagePath,
        string targetLocation,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLocation);

        if (_serializer is null)
        {
            return Result.Failure<PackageImportResult>(PackageErrors.NoSerializer);
        }

        var staging = CreateTempDirectory();
        try
        {
            WorkspacePackageContents contents;
            try
            {
                contents = await _serializer.LoadAsync(packagePath, staging, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is InvalidDataException or FileNotFoundException)
            {
                return Result.Failure<PackageImportResult>(PackageErrors.Unreadable(packagePath));
            }

            var formatCheck = ValidateFormat(contents.Manifest.FormatVersion);
            if (formatCheck.IsFailure)
            {
                return Result.Failure<PackageImportResult>(formatCheck.Error);
            }

            var schemaCheck = SchemaVersionValidator.Validate(contents.Manifest.WorkspaceSchemaVersion);
            if (schemaCheck.IsFailure)
            {
                return Result.Failure<PackageImportResult>(schemaCheck.Error);
            }

            // Release any open workspace so the target file is unlocked before we write over it.
            if (_session.IsOpen)
            {
                await _workspaceService.CloseAsync(cancellationToken).ConfigureAwait(false);
            }

            InstallDatabase(contents.DatabasePath, targetLocation);
            InstallAttachments(contents.AttachmentsDirectory, targetLocation);

            var opened = await _workspaceService.OpenAsync(targetLocation, cancellationToken).ConfigureAwait(false);
            if (opened.IsFailure)
            {
                return Result.Failure<PackageImportResult>(opened.Error);
            }

            var secretsNeedReprompt = contents.Manifest.Secrets.MachineBound
                && !string.Equals(
                    contents.Manifest.Secrets.KeyFingerprint,
                    _keyStore.GetKeyFingerprint(),
                    StringComparison.Ordinal);

            var installed = _installedPlugins.InstalledPluginIds;
            var missing = contents.Manifest.Plugins
                .Select(p => p.PluginId)
                .Where(id => !installed.Contains(id))
                .ToList();

            return Result.Success(new PackageImportResult(
                opened.Value.Id,
                targetLocation,
                secretsNeedReprompt,
                missing));
        }
        finally
        {
            TryDeleteDirectory(staging);
        }
    }

    private async Task<PackageManifest> BuildManifestAsync(Domain.Entities.Workspace workspace, CancellationToken cancellationToken)
    {
        var deps = await _packages.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var plugins = deps
            .Select(d => new PackagePluginDependency(d.PluginId, d.PluginName, d.Version))
            .ToList();

        return new PackageManifest(
            PackageManifest.CurrentFormatVersion,
            workspace.SchemaVersion,
            AppVersion,
            workspace.Id,
            workspace.Name,
            _clock.UtcNow,
            plugins,
            new SecretBinding(MachineBound: true, KeyFingerprint: _keyStore.GetKeyFingerprint()));
    }

    private static Result ValidateFormat(string formatVersion)
    {
        if (!Version.TryParse(formatVersion, out var version))
        {
            return Result.Failure(new Error("package.unreadable", $"Package format version '{formatVersion}' is invalid."));
        }

        var current = Version.Parse(PackageManifest.CurrentFormatVersion);
        return version.Major > current.Major
            ? Result.Failure(PackageErrors.FormatTooNew(formatVersion, PackageManifest.CurrentFormatVersion))
            : Result.Success();
    }

    private static void InstallDatabase(string sourceDatabasePath, string targetLocation)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(targetLocation));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // The caller closes any open workspace first; the storage provider clears the SQLite
        // connection pool on close, so the target file handle is already released here.
        DeleteIfExists(targetLocation);
        DeleteIfExists(targetLocation + "-wal");
        DeleteIfExists(targetLocation + "-shm");

        File.Copy(sourceDatabasePath, targetLocation, overwrite: true);
    }

    private static void InstallAttachments(string? sourceAttachmentsDirectory, string targetLocation)
    {
        var targetDir = WorkspaceAttachmentPaths.ForWorkspace(targetLocation);
        if (Directory.Exists(targetDir))
        {
            Directory.Delete(targetDir, recursive: true);
        }

        if (sourceAttachmentsDirectory is null || !Directory.Exists(sourceAttachmentsDirectory))
        {
            return;
        }

        CopyDirectory(sourceAttachmentsDirectory, targetDir);
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var destination = Path.Combine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "ats-package", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup of a temp directory; never fail the operation over it.
        }
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
