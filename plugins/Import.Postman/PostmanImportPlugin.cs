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
/// Imports a Postman Collection (v2.x). The collection becomes one <see cref="Service"/>; each request
/// (including those nested in folders) becomes an <see cref="Endpoint"/>.
/// </summary>
public sealed class PostmanImporter : IImporter
{
    public string Format => "postman";

    public bool CanImport(ImportSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (string.Equals(source.Format, Format, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Postman collections declare their schema URL under info.schema.
        return source.Content is { } content
            && content.Contains("schema.getpostman.com", StringComparison.OrdinalIgnoreCase);
    }

    public Task<ImportResult> ImportAsync(ImportSource source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(source.Content))
        {
            throw new InvalidOperationException("No Postman collection was provided to import.");
        }

        return Task.FromResult(PostmanCollectionParser.Parse(source.Content));
    }
}
