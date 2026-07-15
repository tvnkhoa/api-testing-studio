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
/// Imports a single <c>curl</c> command into one <see cref="Service"/> (the host) plus one
/// <see cref="Endpoint"/> (method, path, headers, body).
/// </summary>
public sealed class CurlImporter : IImporter
{
    public string Format => "curl";

    public bool CanImport(ImportSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (string.Equals(source.Format, Format, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return source.Content is { } content
            && content.TrimStart().StartsWith("curl", StringComparison.OrdinalIgnoreCase);
    }

    public Task<ImportResult> ImportAsync(ImportSource source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(source.Content))
        {
            throw new InvalidOperationException("No cURL command was provided to import.");
        }

        return Task.FromResult(CurlCommandParser.Parse(source.Content));
    }
}
