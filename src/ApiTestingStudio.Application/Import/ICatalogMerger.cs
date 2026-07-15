using ApiTestingStudio.Plugin.Abstractions.Importing;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Import;

/// <summary>
/// Merges a parsed <see cref="ImportResult"/> into the currently open workspace's catalog as a single
/// transaction: either every service/endpoint is committed or none is. Implemented in Infrastructure
/// (the catalog repositories commit per-operation, so a transactional merge needs a dedicated adapter
/// that batches all writes into one unit of work).
/// </summary>
public interface ICatalogMerger
{
    Task<Result<ImportSummary>> MergeAsync(
        ImportResult result,
        ImportOptions options,
        CancellationToken cancellationToken = default);
}
