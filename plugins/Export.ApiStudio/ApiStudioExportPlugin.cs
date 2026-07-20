using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Plugin.Abstractions.Exporting;
using ApiTestingStudio.Plugin.Abstractions.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTestingStudio.Export.ApiStudio;

/// <summary>
/// Plugin module for the native <c>.apistudio</c> package format
/// (manifest.json + database.sqlite + attachments/). This is the ONLY supported export target.
/// </summary>
public sealed class ApiStudioExportPluginModule : IPluginModule
{
    public string Name => "Export.ApiStudio";

    public Version Version => new(1, 0, 0);

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IExporter, ApiStudioExporter>();
        services.AddSingleton<IWorkspaceSerializer, ApiStudioPackageSerializer>();
    }
}

/// <summary>Declares the <c>.apistudio</c> format for the orchestrator and UI to select.</summary>
public sealed class ApiStudioExporter : IExporter
{
    public string Format => ApiStudioPackageSerializer.FormatId;

    public string DisplayName => "API Testing Studio Package";

    public string FileExtension => ".apistudio";
}

/// <summary>
/// Pure, offline reader/writer of the <c>.apistudio</c> package. Delegates byte I/O to
/// <see cref="WorkspacePackager"/> / <see cref="WorkspaceUnpacker"/>. Orchestration (DB maintenance,
/// manifest assembly, install/open) lives in the Application layer. See ADR-0012.
/// </summary>
public sealed class ApiStudioPackageSerializer : IWorkspaceSerializer
{
    internal const string FormatId = "apistudio";

    public string Format => FormatId;

    public Task SaveAsync(WorkspacePackageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return WorkspacePackager.PackAsync(request, cancellationToken);
    }

    public Task<WorkspacePackageContents> LoadAsync(
        string packagePath,
        string stagingDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingDirectory);
        return WorkspaceUnpacker.UnpackAsync(packagePath, stagingDirectory, cancellationToken);
    }

    public Task<PackageManifest> ReadManifestAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
        return WorkspaceUnpacker.ReadManifestAsync(packagePath, cancellationToken);
    }
}
