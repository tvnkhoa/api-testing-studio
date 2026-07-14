using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Plugin.Abstractions.Importing;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTestingStudio.Import.Curl;

/// <summary>Plugin module for importing cURL commands. Registered via plugin discovery.</summary>
public sealed class CurlImportPluginModule : IPluginModule
{
    public string Name => "Import.Curl";

    public Version Version => new(1, 0, 0);

    public void ConfigureServices(IServiceCollection services)
        => services.AddSingleton<IImporter, CurlImporter>();
}

/// <summary>
/// Placeholder cURL importer. Parsing is implemented in the Import System sprint (Sprint 07).
/// </summary>
public sealed class CurlImporter : IImporter
{
    public string Format => "curl";

    public bool CanImport(ImportSource source) => false;

    public Task<ImportResult> ImportAsync(ImportSource source, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("cURL import is delivered in Sprint 07 (Import System).");
}
