using ApiTestingStudio.Application.Import;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// <see cref="ICatalogMerger"/> that commits a parsed <see cref="Plugin.Abstractions.Importing.ImportResult"/>
/// into the open workspace as a single transaction. Unlike the per-operation catalog repositories, this
/// opens one <see cref="WorkspaceDbContext"/> and batches every insert/update into one
/// <c>SaveChangesAsync</c> inside an explicit transaction, so a partial failure rolls back completely.
/// </summary>
public sealed class CatalogMerger : ICatalogMerger
{
    private readonly WorkspaceSession _session;
    private readonly ILogger<CatalogMerger> _logger;

    public CatalogMerger(WorkspaceSession session, ILogger<CatalogMerger> logger)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ImportSummary>> MergeAsync(
        Plugin.Abstractions.Importing.ImportResult result,
        ImportOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(options);

        if (!_session.IsOpen || _session.Current is not { } workspace || _session.ConnectionString is not { } connectionString)
        {
            return Result.Failure<ImportSummary>(ImportErrors.NoWorkspaceOpen);
        }

        await using var context = WorkspaceContextFactory.Create(connectionString);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var summary = await ApplyAsync(context, workspace.Id, result, options, cancellationToken).ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(summary);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            _logger.LogWarning(ex, "Import merge failed and was rolled back.");
            return Result.Failure<ImportSummary>(ImportErrors.MergeFailed(ex.Message));
        }
    }

    private static async Task<ImportSummary> ApplyAsync(
        WorkspaceDbContext context,
        Guid workspaceId,
        Plugin.Abstractions.Importing.ImportResult result,
        ImportOptions options,
        CancellationToken cancellationToken)
    {
        // Read untracked: overwrites re-attach freshly-built instances via Update, which would clash
        // with tracked originals sharing the same key.
        var existingServices = await context.Services
            .AsNoTracking()
            .Where(s => s.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingServiceIds = existingServices.Select(s => s.Id).ToList();
        var existingEndpoints = await context.Endpoints
            .AsNoTracking()
            .Where(e => existingServiceIds.Contains(e.ServiceId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var endpointsByService = result.Endpoints
            .GroupBy(e => e.ServiceId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var servicesCreated = 0;
        var servicesUpdated = 0;
        var endpointsCreated = 0;
        var endpointsUpdated = 0;
        var endpointsSkipped = 0;

        var nextServiceSort = existingServices.Count;

        foreach (var importedService in result.Services)
        {
            var existingService = existingServices.FirstOrDefault(
                s => NameEquals(s.Name, importedService.Name) && UrlEquals(s.BaseUrl, importedService.BaseUrl));

            Guid targetServiceId;
            var serviceIsNew = existingService is null;

            if (existingService is null)
            {
                var newService = importedService with
                {
                    WorkspaceId = workspaceId,
                    SortOrder = nextServiceSort++,
                };
                await context.Services.AddAsync(newService, cancellationToken).ConfigureAwait(false);
                targetServiceId = newService.Id;
                servicesCreated++;
            }
            else
            {
                targetServiceId = existingService.Id;
            }

            // Endpoints already under the target service, keyed for matching + sort ordering.
            var siblingEndpoints = existingEndpoints.Where(e => e.ServiceId == targetServiceId).ToList();
            var existingByKey = siblingEndpoints.ToDictionary(EndpointKey, e => e, StringComparer.OrdinalIgnoreCase);
            var nextEndpointSort = siblingEndpoints.Count(e => e.FolderId is null);
            var serviceChanged = false;

            var importedEndpoints = endpointsByService.TryGetValue(importedService.Id, out var eps) ? eps : [];
            foreach (var importedEndpoint in importedEndpoints)
            {
                if (existingByKey.TryGetValue(EndpointKey(importedEndpoint), out var match))
                {
                    if (!options.OverwriteExisting)
                    {
                        endpointsSkipped++;
                        continue;
                    }

                    var updated = match with
                    {
                        Name = importedEndpoint.Name,
                        Description = importedEndpoint.Description,
                        DefaultHeaders = importedEndpoint.DefaultHeaders,
                        DefaultBody = importedEndpoint.DefaultBody,
                    };
                    context.Endpoints.Update(updated);
                    endpointsUpdated++;
                    serviceChanged = true;
                    continue;
                }

                var newEndpoint = importedEndpoint with
                {
                    ServiceId = targetServiceId,
                    FolderId = null,
                    SortOrder = nextEndpointSort++,
                };
                await context.Endpoints.AddAsync(newEndpoint, cancellationToken).ConfigureAwait(false);
                endpointsCreated++;
                serviceChanged = true;
            }

            if (!serviceIsNew && serviceChanged)
            {
                servicesUpdated++;
            }
        }

        return new ImportSummary(servicesCreated, servicesUpdated, endpointsCreated, endpointsUpdated, endpointsSkipped);
    }

    private static string EndpointKey(Endpoint endpoint) => $"{endpoint.Method} {endpoint.Path}";

    private static bool NameEquals(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static bool UrlEquals(string? a, string? b) =>
        string.Equals(a?.TrimEnd('/'), b?.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
}
