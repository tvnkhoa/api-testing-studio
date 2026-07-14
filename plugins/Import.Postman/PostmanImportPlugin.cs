using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Plugin.Abstractions.Importing;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTestingStudio.Import.Postman;

/// <summary>Plugin module for importing Postman collections and environments.</summary>
public sealed class PostmanImportPluginModule : IPluginModule
{
    public string Name => "Import.Postman";

    public Version Version => new(1, 0, 0);

    public void ConfigureServices(IServiceCollection services)
        => services.AddSingleton<IImporter, PostmanImporter>();
}

/// <summary>
/// Placeholder Postman importer. Parsing is implemented in the Import System sprint (Sprint 07).
/// </summary>
public sealed class PostmanImporter : IImporter
{
    public string Format => "postman";

    public bool CanImport(ImportSource source) => false;

    public Task<ImportResult> ImportAsync(ImportSource source, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Postman import is delivered in Sprint 07 (Import System).");
}
