using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.ServiceCatalog;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Plugin.Abstractions.Importing;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Import;

/// <inheritdoc />
public sealed class ImportOrchestrator : IImportOrchestrator
{
    private readonly IReadOnlyList<IImporter> _importers;
    private readonly ISourceFormatDetector _detector;
    private readonly IDefinitionFetcher _fetcher;
    private readonly ICatalogMerger _merger;
    private readonly IServiceExplorerService _catalog;
    private readonly IWorkspaceSession _session;

    public ImportOrchestrator(
        IEnumerable<IImporter> importers,
        ISourceFormatDetector detector,
        IDefinitionFetcher fetcher,
        ICatalogMerger merger,
        IServiceExplorerService catalog,
        IWorkspaceSession session)
    {
        ArgumentNullException.ThrowIfNull(importers);
        _importers = importers.ToList();
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
        _fetcher = fetcher ?? throw new ArgumentNullException(nameof(fetcher));
        _merger = merger ?? throw new ArgumentNullException(nameof(merger));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public async Task<Result<ImportPreview>> PreviewAsync(ImportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_session.IsOpen)
        {
            return Result.Failure<ImportPreview>(ImportErrors.NoWorkspaceOpen);
        }

        // 1. Resolve the raw content (fetch the URL only when no content was supplied directly).
        var content = request.Content;
        var resolvedUri = request.Uri;
        string? fetchedFormat = null;

        if (string.IsNullOrWhiteSpace(content))
        {
            if (string.IsNullOrWhiteSpace(request.Uri))
            {
                return Result.Failure<ImportPreview>(ImportErrors.NothingToImport);
            }

            var fetch = await _fetcher.FetchAsync(request.Uri, cancellationToken).ConfigureAwait(false);
            if (fetch.IsFailure)
            {
                return Result.Failure<ImportPreview>(fetch.Error);
            }

            content = fetch.Value.Content;
            resolvedUri = fetch.Value.ResolvedUri;
            fetchedFormat = fetch.Value.Format;
        }

        // 2. Determine the format and select the importer.
        var format = request.Format
            ?? fetchedFormat
            ?? _detector.Detect(content, request.FileName, resolvedUri);

        var source = new ImportSource(format ?? string.Empty, content, resolvedUri);
        var importer = SelectImporter(format, source);
        if (importer is null)
        {
            return Result.Failure<ImportPreview>(ImportErrors.UnknownFormat);
        }

        // 3. Parse. Importers throw on malformed input; wrap into a typed, actionable failure.
        ImportResult result;
        try
        {
            result = await importer.ImportAsync(source, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<ImportPreview>(ImportErrors.ParseFailed(ex.Message));
        }

        if (result is null || (result.Services.Count == 0 && result.Endpoints.Count == 0))
        {
            return Result.Failure<ImportPreview>(ImportErrors.NothingFound);
        }

        // 4. Build the create/update preview against the existing catalog.
        var tree = await _catalog.LoadTreeAsync(cancellationToken).ConfigureAwait(false);
        if (tree.IsFailure)
        {
            return Result.Failure<ImportPreview>(tree.Error);
        }

        var preview = BuildPreview(importer.Format, result, tree.Value);
        return Result.Success(preview);
    }

    public Task<Result<ImportSummary>> CommitAsync(ImportPreview preview, ImportOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preview);
        ArgumentNullException.ThrowIfNull(options);

        return _merger.MergeAsync(preview.Result, options, cancellationToken);
    }

    private IImporter? SelectImporter(string? format, ImportSource source)
    {
        if (!string.IsNullOrWhiteSpace(format))
        {
            var byFormat = _importers.FirstOrDefault(
                i => string.Equals(i.Format, format, StringComparison.OrdinalIgnoreCase));
            if (byFormat is not null)
            {
                return byFormat;
            }
        }

        return _importers.FirstOrDefault(i => i.CanImport(source));
    }

    private static ImportPreview BuildPreview(string format, ImportResult result, ServiceCatalogTree existingTree)
    {
        var endpointsByService = result.Endpoints
            .GroupBy(e => e.ServiceId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var services = new List<ParsedService>(result.Services.Count);
        var totalEndpoints = 0;

        foreach (var service in result.Services)
        {
            var existing = existingTree.Services.FirstOrDefault(
                s => NameEquals(s.Name, service.Name) && UrlEquals(s.BaseUrl, service.BaseUrl));

            var existingPaths = existing is null
                ? new HashSet<string>()
                : Flatten(existing).Select(EndpointKey).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var endpoints = endpointsByService.TryGetValue(service.Id, out var eps) ? eps : [];
            var parsedEndpoints = endpoints
                .Select(e => new ParsedEndpoint(
                    e.Name,
                    e.Method,
                    e.Path,
                    existingPaths.Contains(EndpointKey(e.Method.ToString(), e.Path))
                        ? ImportChangeKind.Update
                        : ImportChangeKind.Create))
                .ToList();

            totalEndpoints += parsedEndpoints.Count;
            services.Add(new ParsedService(
                service.Name,
                service.BaseUrl,
                existing is null ? ImportChangeKind.Create : ImportChangeKind.Update,
                parsedEndpoints));
        }

        return new ImportPreview
        {
            Format = format,
            Services = services,
            EndpointCount = totalEndpoints,
            Result = result,
        };
    }

    private static IEnumerable<EndpointNode> Flatten(ServiceNode service)
    {
        foreach (var endpoint in service.Endpoints)
        {
            yield return endpoint;
        }

        foreach (var folder in service.Folders)
        {
            foreach (var endpoint in Flatten(folder))
            {
                yield return endpoint;
            }
        }
    }

    private static IEnumerable<EndpointNode> Flatten(FolderNode folder)
    {
        foreach (var endpoint in folder.Endpoints)
        {
            yield return endpoint;
        }

        foreach (var child in folder.Folders)
        {
            foreach (var endpoint in Flatten(child))
            {
                yield return endpoint;
            }
        }
    }

    private static string EndpointKey(EndpointNode endpoint) => EndpointKey(endpoint.Method.ToString(), endpoint.Path);

    private static string EndpointKey(string method, string path) => $"{method} {path}";

    private static bool NameEquals(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static bool UrlEquals(string? a, string? b) =>
        string.Equals(a?.TrimEnd('/'), b?.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
}
