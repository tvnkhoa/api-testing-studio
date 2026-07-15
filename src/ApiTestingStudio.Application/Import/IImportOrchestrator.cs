using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Import;

/// <summary>
/// Drives the import pipeline: resolve source (fetch a URL if needed) → detect format → select the
/// importer → parse → build a preview. On confirmation, commits the parsed result into the catalog.
/// </summary>
public interface IImportOrchestrator
{
    /// <summary>Parses the source and returns a preview of what would be created/updated.</summary>
    Task<Result<ImportPreview>> PreviewAsync(ImportRequest request, CancellationToken cancellationToken = default);

    /// <summary>Commits a previously-built preview into the open workspace's catalog transactionally.</summary>
    Task<Result<ImportSummary>> CommitAsync(
        ImportPreview preview,
        ImportOptions options,
        CancellationToken cancellationToken = default);
}
