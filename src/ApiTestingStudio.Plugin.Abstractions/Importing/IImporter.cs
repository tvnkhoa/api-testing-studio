using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Plugin.Abstractions.Importing;

/// <summary>Describes where import data comes from (raw text, a file path, or a URL).</summary>
public sealed record ImportSource(string Format, string? Content = null, string? Uri = null);

/// <summary>The services and endpoints produced by an import operation.</summary>
public sealed record ImportResult(
    IReadOnlyList<Service> Services,
    IReadOnlyList<Endpoint> Endpoints)
{
    public static ImportResult Empty { get; } = new([], []);
}

/// <summary>
/// Converts an external API description (cURL, OpenAPI, Postman, Scalar, …) into workspace
/// entities. Implemented by <c>Import.*</c> plugins.
/// </summary>
public interface IImporter
{
    /// <summary>A stable identifier for the format this importer handles (e.g. "curl").</summary>
    string Format { get; }

    /// <summary>Returns true if this importer can handle the given source.</summary>
    bool CanImport(ImportSource source);

    /// <summary>Parses the source into workspace entities.</summary>
    Task<ImportResult> ImportAsync(ImportSource source, CancellationToken cancellationToken = default);
}
