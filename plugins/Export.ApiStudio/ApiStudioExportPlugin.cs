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
        services.AddSingleton<ApiStudioPackageSerializer>();
        services.AddSingleton<IExporter>(sp => sp.GetRequiredService<ApiStudioPackageSerializer>());
        services.AddSingleton<IWorkspaceSerializer>(sp => sp.GetRequiredService<ApiStudioPackageSerializer>());
    }
}

/// <summary>
/// Placeholder <c>.apistudio</c> reader/writer. ZIP packaging (manifest + sqlite + attachments)
/// is implemented in the Packaging &amp; Polish sprint (Sprint 14).
/// </summary>
public sealed class ApiStudioPackageSerializer : IExporter, IWorkspaceSerializer
{
    public string Format => "apistudio";

    public Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException(".apistudio export is delivered in Sprint 14 (Packaging & Polish).");

    public Task SaveAsync(Guid workspaceId, string packagePath, CancellationToken cancellationToken = default)
        => throw new NotImplementedException(".apistudio save is delivered in Sprint 14 (Packaging & Polish).");

    public Task<Guid> LoadAsync(string packagePath, CancellationToken cancellationToken = default)
        => throw new NotImplementedException(".apistudio load is delivered in Sprint 14 (Packaging & Polish).");
}
