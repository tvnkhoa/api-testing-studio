using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Plugin.Abstractions.Importing;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTestingStudio.Import.Scalar;

/// <summary>Plugin module for importing Scalar API references (.NET 9 / .NET 10).</summary>
public sealed class ScalarImportPluginModule : IPluginModule
{
    public string Name => "Import.Scalar";

    public Version Version => new(1, 0, 0);

    public void ConfigureServices(IServiceCollection services)
        => services.AddSingleton<IImporter, ScalarImporter>();
}

/// <summary>
/// Placeholder Scalar importer. Parsing is implemented in the Import System sprint (Sprint 07).
/// </summary>
public sealed class ScalarImporter : IImporter
{
    public string Format => "scalar";

    public bool CanImport(ImportSource source) => false;

    public Task<ImportResult> ImportAsync(ImportSource source, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Scalar import is delivered in Sprint 07 (Import System).");
}
