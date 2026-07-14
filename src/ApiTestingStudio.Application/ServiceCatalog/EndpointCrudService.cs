using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.ServiceCatalog;

/// <summary>
/// Endpoint create/edit/duplicate/delete/move/reorder over the catalog repositories. Holds no
/// persistence types; workspace scope comes from <see cref="IWorkspaceSession"/>.
/// </summary>
public sealed class EndpointCrudService : IEndpointCrudService
{
    private readonly IEndpointRepository _endpoints;
    private readonly IServiceRepository _services;
    private readonly IEndpointFolderRepository _folders;
    private readonly IWorkspaceSession _session;

    public EndpointCrudService(
        IEndpointRepository endpoints,
        IServiceRepository services,
        IEndpointFolderRepository folders,
        IWorkspaceSession session)
    {
        _endpoints = endpoints;
        _services = services;
        _folders = folders;
        _session = session;
    }

    public async Task<Result<Endpoint>> CreateEndpointAsync(Guid serviceId, Guid? folderId, EndpointDraft draft, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (!_session.IsOpen)
        {
            return Result.Failure<Endpoint>(ServiceCatalogErrors.NoWorkspaceOpen);
        }

        var validation = Validate(draft);
        if (validation.IsFailure)
        {
            return Result.Failure<Endpoint>(validation.Error);
        }

        var service = await _services.GetAsync(serviceId, cancellationToken).ConfigureAwait(false);
        if (service is null)
        {
            return Result.Failure<Endpoint>(ServiceCatalogErrors.ServiceNotFound(serviceId));
        }

        var sortOrder = await CountSiblingsAsync(serviceId, folderId, cancellationToken).ConfigureAwait(false);
        var endpoint = new Endpoint
        {
            ServiceId = serviceId,
            FolderId = folderId,
            Name = draft.Name.Trim(),
            Method = draft.Method,
            Path = draft.Path.Trim(),
            Description = string.IsNullOrWhiteSpace(draft.Description) ? null : draft.Description.Trim(),
            SortOrder = sortOrder,
        };

        await _endpoints.AddAsync(endpoint, cancellationToken).ConfigureAwait(false);
        return Result.Success(endpoint);
    }

    public async Task<Result<Endpoint>> UpdateEndpointAsync(Guid id, EndpointDraft draft, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (!_session.IsOpen)
        {
            return Result.Failure<Endpoint>(ServiceCatalogErrors.NoWorkspaceOpen);
        }

        var validation = Validate(draft);
        if (validation.IsFailure)
        {
            return Result.Failure<Endpoint>(validation.Error);
        }

        var existing = await _endpoints.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return Result.Failure<Endpoint>(ServiceCatalogErrors.EndpointNotFound(id));
        }

        var updated = existing with
        {
            Name = draft.Name.Trim(),
            Method = draft.Method,
            Path = draft.Path.Trim(),
            Description = string.IsNullOrWhiteSpace(draft.Description) ? null : draft.Description.Trim(),
        };
        await _endpoints.UpdateAsync(updated, cancellationToken).ConfigureAwait(false);
        return Result.Success(updated);
    }

    public async Task<Result<Endpoint>> DuplicateEndpointAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_session.IsOpen)
        {
            return Result.Failure<Endpoint>(ServiceCatalogErrors.NoWorkspaceOpen);
        }

        var source = await _endpoints.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (source is null)
        {
            return Result.Failure<Endpoint>(ServiceCatalogErrors.EndpointNotFound(id));
        }

        var sortOrder = await CountSiblingsAsync(source.ServiceId, source.FolderId, cancellationToken).ConfigureAwait(false);
        var copy = source with
        {
            Id = Guid.NewGuid(),
            Name = $"{source.Name} (copy)",
            SortOrder = sortOrder,
        };
        await _endpoints.AddAsync(copy, cancellationToken).ConfigureAwait(false);
        return Result.Success(copy);
    }

    public async Task<Result> DeleteEndpointAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_session.IsOpen)
        {
            return Result.Failure(ServiceCatalogErrors.NoWorkspaceOpen);
        }

        var existing = await _endpoints.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return Result.Failure(ServiceCatalogErrors.EndpointNotFound(id));
        }

        await _endpoints.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> MoveEndpointAsync(Guid id, Guid? targetFolderId, CancellationToken cancellationToken = default)
    {
        if (!_session.IsOpen)
        {
            return Result.Failure(ServiceCatalogErrors.NoWorkspaceOpen);
        }

        var endpoint = await _endpoints.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (endpoint is null)
        {
            return Result.Failure(ServiceCatalogErrors.EndpointNotFound(id));
        }

        if (targetFolderId is { } folderId)
        {
            var folder = await _folders.GetAsync(folderId, cancellationToken).ConfigureAwait(false);
            if (folder is null || folder.ServiceId != endpoint.ServiceId)
            {
                return Result.Failure(ServiceCatalogErrors.FolderNotFound(folderId));
            }
        }

        var sortOrder = await CountSiblingsAsync(endpoint.ServiceId, targetFolderId, cancellationToken).ConfigureAwait(false);
        await _endpoints.UpdateAsync(endpoint with { FolderId = targetFolderId, SortOrder = sortOrder }, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> ReorderEndpointAsync(Guid id, bool up, CancellationToken cancellationToken = default)
    {
        if (!_session.IsOpen)
        {
            return Result.Failure(ServiceCatalogErrors.NoWorkspaceOpen);
        }

        var endpoint = await _endpoints.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (endpoint is null)
        {
            return Result.Failure(ServiceCatalogErrors.EndpointNotFound(id));
        }

        var siblings = (await _endpoints.GetByServiceAsync(endpoint.ServiceId, cancellationToken).ConfigureAwait(false))
            .Where(e => e.FolderId == endpoint.FolderId)
            .ToList();

        var index = siblings.FindIndex(e => e.Id == id);
        var target = up ? index - 1 : index + 1;
        if (target >= 0 && target < siblings.Count)
        {
            (siblings[index], siblings[target]) = (siblings[target], siblings[index]);
        }

        for (var i = 0; i < siblings.Count; i++)
        {
            if (siblings[i].SortOrder != i)
            {
                await _endpoints.UpdateAsync(siblings[i] with { SortOrder = i }, cancellationToken).ConfigureAwait(false);
            }
        }

        return Result.Success();
    }

    private async Task<int> CountSiblingsAsync(Guid serviceId, Guid? folderId, CancellationToken cancellationToken) =>
        (await _endpoints.GetByServiceAsync(serviceId, cancellationToken).ConfigureAwait(false))
            .Count(e => e.FolderId == folderId);

    private static Result Validate(EndpointDraft draft)
    {
        if (string.IsNullOrWhiteSpace(draft.Name))
        {
            return Result.Failure(ServiceCatalogErrors.NameRequired);
        }

        return string.IsNullOrWhiteSpace(draft.Path)
            ? Result.Failure(ServiceCatalogErrors.PathRequired)
            : Result.Success();
    }
}
