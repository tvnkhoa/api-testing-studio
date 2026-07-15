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
/// Imports an OpenAPI 3.x / Swagger 2.0 document. The orchestrator resolves URLs to text first, so
/// this importer only ever parses <see cref="ImportSource.Content"/> (JSON or YAML).
/// </summary>
public sealed class OpenApiImporter : IImporter
{
    public string Format => "openapi";

    public bool CanImport(ImportSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (string.Equals(source.Format, Format, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var content = source.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        // Cheap sniff: an OpenAPI/Swagger document declares its version at the root, in JSON or YAML.
        return content.Contains("\"openapi\"", StringComparison.OrdinalIgnoreCase)
            || content.Contains("\"swagger\"", StringComparison.OrdinalIgnoreCase)
            || content.Contains("openapi:", StringComparison.OrdinalIgnoreCase)
            || content.Contains("swagger:", StringComparison.OrdinalIgnoreCase);
    }

    public Task<ImportResult> ImportAsync(ImportSource source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(source.Content))
        {
            throw new InvalidOperationException("No OpenAPI content was provided to import.");
        }

        var result = OpenApiEndpointMapper.Map(source.Content, fallbackName: source.Uri);
        return Task.FromResult(result);
    }
}
