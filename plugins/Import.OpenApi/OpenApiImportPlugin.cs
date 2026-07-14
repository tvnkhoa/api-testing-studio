using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Plugin.Abstractions.Importing;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTestingStudio.Import.OpenApi;

/// <summary>Plugin module for importing OpenAPI / Swagger documents (JSON, YAML, URL).</summary>
public sealed class OpenApiImportPluginModule : IPluginModule
{
    public string Name => "Import.OpenApi";

    public Version Version => new(1, 0, 0);

    public void ConfigureServices(IServiceCollection services)
        => services.AddSingleton<IImporter, OpenApiImporter>();
}

/// <summary>
/// Placeholder OpenAPI importer. Parsing is implemented in the Import System sprint (Sprint 07).
/// </summary>
public sealed class OpenApiImporter : IImporter
{
    public string Format => "openapi";

    public bool CanImport(ImportSource source) => false;

    public Task<ImportResult> ImportAsync(ImportSource source, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("OpenAPI import is delivered in Sprint 07 (Import System).");
}
