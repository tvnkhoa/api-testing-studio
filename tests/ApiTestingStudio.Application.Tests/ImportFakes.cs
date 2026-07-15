using ApiTestingStudio.Application.Import;
using ApiTestingStudio.Application.ServiceCatalog;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Plugin.Abstractions.Importing;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Tests;

/// <summary>An importer that reports a fixed format and returns a scripted result (or throws).</summary>
internal sealed class FakeImporter : IImporter
{
    private readonly ImportResult _result;
    private readonly Exception? _throw;

    public FakeImporter(string format, ImportResult? result = null, Exception? toThrow = null)
    {
        Format = format;
        _result = result ?? ImportResult.Empty;
        _throw = toThrow;
    }

    public string Format { get; }

    public bool CanImportResult { get; set; } = true;

    public bool CanImport(ImportSource source) => CanImportResult;

    public Task<ImportResult> ImportAsync(ImportSource source, CancellationToken cancellationToken = default)
    {
        if (_throw is not null)
        {
            throw _throw;
        }

        return Task.FromResult(_result);
    }
}

/// <summary>Scripted definition fetcher.</summary>
internal sealed class FakeDefinitionFetcher : IDefinitionFetcher
{
    public Result<FetchedDefinition> Next { get; set; } =
        Result.Failure<FetchedDefinition>(ImportErrors.FetchFailed("not configured"));

    public string? LastUri { get; private set; }

    public Task<Result<FetchedDefinition>> FetchAsync(string uri, CancellationToken cancellationToken = default)
    {
        LastUri = uri;
        return Task.FromResult(Next);
    }
}

/// <summary>Records the last merge and returns a scripted summary.</summary>
internal sealed class FakeCatalogMerger : ICatalogMerger
{
    public int MergeCount { get; private set; }

    public ImportResult? LastResult { get; private set; }

    public ImportOptions? LastOptions { get; private set; }

    public Result<ImportSummary> Next { get; set; } =
        Result.Success(new ImportSummary(0, 0, 0, 0, 0));

    public Task<Result<ImportSummary>> MergeAsync(ImportResult result, ImportOptions options, CancellationToken cancellationToken = default)
    {
        MergeCount++;
        LastResult = result;
        LastOptions = options;
        return Task.FromResult(Next);
    }
}

/// <summary>Minimal catalog reader: only <see cref="LoadTreeAsync"/> is exercised by the orchestrator.</summary>
internal sealed class FakeCatalogReader : IServiceExplorerService
{
    private readonly ServiceCatalogTree _tree;

    public FakeCatalogReader(ServiceCatalogTree? tree = null) => _tree = tree ?? ServiceCatalogTree.Empty;

    public Task<Result<ServiceCatalogTree>> LoadTreeAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success(_tree));

    public Task<Result<Service>> CreateServiceAsync(ServiceDraft draft, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<Result<Service>> UpdateServiceAsync(Guid id, ServiceDraft draft, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<Result> DeleteServiceAsync(Guid id, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<Result<EndpointFolder>> CreateFolderAsync(Guid serviceId, Guid? parentFolderId, string name, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<Result<EndpointFolder>> RenameFolderAsync(Guid id, string name, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<Result> DeleteFolderAsync(Guid id, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<Result> ReorderServiceAsync(Guid id, bool up, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<Result> ReorderFolderAsync(Guid id, bool up, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
