using ApiTestingStudio.Import.OpenApi;
using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Plugin.Abstractions.Importing;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTestingStudio.Import.Scalar;

/// <summary>Plugin module for importing a Scalar (.NET 9 / .NET 10) API reference.</summary>
public sealed class ScalarImportPluginModule : IPluginModule
{
    public string Name => "Import.Scalar";

    public Version Version => new(1, 0, 0);

    public void ConfigureServices(IServiceCollection services)
        => services.AddSingleton<IImporter, ScalarImporter>();
}

/// <summary>
/// Imports a Scalar API reference. A Scalar endpoint is a thin UI over a standard OpenAPI document,
/// so once the orchestrator has resolved the underlying OpenAPI text this importer reuses the shared
/// <see cref="OpenApiEndpointMapper"/>.
/// </summary>
public sealed class ScalarImporter : IImporter
{
    public string Format => "scalar";

    public bool CanImport(ImportSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (string.Equals(source.Format, Format, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return source.Uri is { } uri && uri.Contains("/scalar", StringComparison.OrdinalIgnoreCase);
    }

    public Task<ImportResult> ImportAsync(ImportSource source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(source.Content))
        {
            throw new InvalidOperationException(
                "No OpenAPI content was resolved from the Scalar reference to import.");
        }

        var result = OpenApiEndpointMapper.Map(source.Content, fallbackName: source.Uri);
        return Task.FromResult(result);
    }
}
