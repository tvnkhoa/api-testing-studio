using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.ServiceCatalog;

/// <summary>
/// Assembles the Service Explorer tree and performs service/folder CRUD + reordering over the catalog
/// repositories. Holds no persistence types; workspace scope comes from <see cref="IWorkspaceSession"/>.
/// </summary>
public sealed class ServiceExplorerService : IServiceExplorerService
{
    private readonly IServiceRepository _services;
    private readonly IEndpointFolderRepository _folders;
    private readonly IEndpointRepository _endpoints;
    private readonly IWorkspaceSession _session;

    public ServiceExplorerService(
        IServiceRepository services,
        IEndpointFolderRepository folders,
        IEndpointRepository endpoints,
        IWorkspaceSession session)
    {
        _services = services;
        _folders = folders;
        _endpoints = endpoints;
        _session = session;
    }

    public async Task<Result<ServiceCatalogTree>> LoadTreeAsync(CancellationToken cancellationToken = default)
    {
        if (_session.Current is not { } workspace)
        {
            return Result.Failure<ServiceCatalogTree>(ServiceCatalogErrors.NoWorkspaceOpen);
        }

        var services = await _services.GetByWorkspaceAsync(workspace.Id, cancellationToken).ConfigureAwait(false);

        var serviceNodes = new List<ServiceNode>(services.Count);
        foreach (var service in services)
        {
            var folders = await _folders.GetByServiceAsync(service.Id, cancellationToken).ConfigureAwait(false);
            var endpoints = await _endpoints.GetByServiceAsync(service.Id, cancellationToken).ConfigureAwait(false);

            // ILookup permits nullable keys (a null parent/folder = directly under the service).
            var foldersByParent = folders.ToLookup(f => f.ParentFolderId);
            var endpointsByFolder = endpoints.ToLookup(e => e.FolderId);

            serviceNodes.Add(new ServiceNode(
                service.Id,
                service.Name,
                service.BaseUrl,
                service.Description,
                service.SortOrder,
                BuildFolders(null, foldersByParent, endpointsByFolder),
                MapEndpoints(endpointsByFolder, null)));
        }

        return Result.Success(new ServiceCatalogTree(serviceNodes));
    }

    public async Task<Result<Service>> CreateServiceAsync(ServiceDraft draft, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (_session.Current is not { } workspace)
        {
            return Result.Failure<Service>(ServiceCatalogErrors.NoWorkspaceOpen);
        }

        var name = draft.Name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return Result.Failure<Service>(ServiceCatalogErrors.NameRequired);
        }

        var siblings = await _services.GetByWorkspaceAsync(workspace.Id, cancellationToken).ConfigureAwait(false);
        var service = new Service
        {
            WorkspaceId = workspace.Id,
            Name = name,
            BaseUrl = Normalize(draft.BaseUrl),
            Description = Normalize(draft.Description),
            SortOrder = siblings.Count,
        };

        await _services.AddAsync(service, cancellationToken).ConfigureAwait(false);
        return Result.Success(service);
    }

    public async Task<Result<Service>> UpdateServiceAsync(Guid id, ServiceDraft draft, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var guard = EnsureOpen<Service>();
        if (guard.IsFailure)
        {
            return guard;
        }

        var name = draft.Name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return Result.Failure<Service>(ServiceCatalogErrors.NameRequired);
        }

        var existing = await _services.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return Result.Failure<Service>(ServiceCatalogErrors.ServiceNotFound(id));
        }

        var updated = existing with
        {
            Name = name,
            BaseUrl = Normalize(draft.BaseUrl),
            Description = Normalize(draft.Description),
        };
        await _services.UpdateAsync(updated, cancellationToken).ConfigureAwait(false);
        return Result.Success(updated);
    }

    public async Task<Result> DeleteServiceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_session.IsOpen)
        {
            return Result.Failure(ServiceCatalogErrors.NoWorkspaceOpen);
        }

        var existing = await _services.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return Result.Failure(ServiceCatalogErrors.ServiceNotFound(id));
        }

        await _services.DeleteCascadeAsync(id, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result<EndpointFolder>> CreateFolderAsync(Guid serviceId, Guid? parentFolderId, string name, CancellationToken cancellationToken = default)
    {
        var guard = EnsureOpen<EndpointFolder>();
        if (guard.IsFailure)
        {
            return guard;
        }

        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return Result.Failure<EndpointFolder>(ServiceCatalogErrors.NameRequired);
        }

        var service = await _services.GetAsync(serviceId, cancellationToken).ConfigureAwait(false);
        if (service is null)
        {
            return Result.Failure<EndpointFolder>(ServiceCatalogErrors.ServiceNotFound(serviceId));
        }

        var siblings = (await _folders.GetByServiceAsync(serviceId, cancellationToken).ConfigureAwait(false))
            .Count(f => f.ParentFolderId == parentFolderId);
        var folder = new EndpointFolder
        {
            ServiceId = serviceId,
            ParentFolderId = parentFolderId,
            Name = trimmed,
            SortOrder = siblings,
        };

        await _folders.AddAsync(folder, cancellationToken).ConfigureAwait(false);
        return Result.Success(folder);
    }

    public async Task<Result<EndpointFolder>> RenameFolderAsync(Guid id, string name, CancellationToken cancellationToken = default)
    {
        var guard = EnsureOpen<EndpointFolder>();
        if (guard.IsFailure)
        {
            return guard;
        }

        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return Result.Failure<EndpointFolder>(ServiceCatalogErrors.NameRequired);
        }

        var existing = await _folders.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return Result.Failure<EndpointFolder>(ServiceCatalogErrors.FolderNotFound(id));
        }

        var updated = existing with { Name = trimmed };
        await _folders.UpdateAsync(updated, cancellationToken).ConfigureAwait(false);
        return Result.Success(updated);
    }

    public async Task<Result> DeleteFolderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_session.IsOpen)
        {
            return Result.Failure(ServiceCatalogErrors.NoWorkspaceOpen);
        }

        var existing = await _folders.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return Result.Failure(ServiceCatalogErrors.FolderNotFound(id));
        }

        await _folders.DeleteCascadeAsync(id, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> ReorderServiceAsync(Guid id, bool up, CancellationToken cancellationToken = default)
    {
        if (_session.Current is not { } workspace)
        {
            return Result.Failure(ServiceCatalogErrors.NoWorkspaceOpen);
        }

        var ordered = await _services.GetByWorkspaceAsync(workspace.Id, cancellationToken).ConfigureAwait(false);
        var reordered = Swap(ordered, id, up, s => s.Id);
        if (reordered is null)
        {
            return Result.Failure(ServiceCatalogErrors.ServiceNotFound(id));
        }

        for (var i = 0; i < reordered.Count; i++)
        {
            if (reordered[i].SortOrder != i)
            {
                await _services.UpdateAsync(reordered[i] with { SortOrder = i }, cancellationToken).ConfigureAwait(false);
            }
        }

        return Result.Success();
    }

    public async Task<Result> ReorderFolderAsync(Guid id, bool up, CancellationToken cancellationToken = default)
    {
        if (!_session.IsOpen)
        {
            return Result.Failure(ServiceCatalogErrors.NoWorkspaceOpen);
        }

        var folder = await _folders.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (folder is null)
        {
            return Result.Failure(ServiceCatalogErrors.FolderNotFound(id));
        }

        var siblings = (await _folders.GetByServiceAsync(folder.ServiceId, cancellationToken).ConfigureAwait(false))
            .Where(f => f.ParentFolderId == folder.ParentFolderId)
            .ToList();
        var reordered = Swap(siblings, id, up, f => f.Id)!;

        for (var i = 0; i < reordered.Count; i++)
        {
            if (reordered[i].SortOrder != i)
            {
                await _folders.UpdateAsync(reordered[i] with { SortOrder = i }, cancellationToken).ConfigureAwait(false);
            }
        }

        return Result.Success();
    }

    private static List<FolderNode> BuildFolders(
        Guid? parentId,
        ILookup<Guid?, EndpointFolder> foldersByParent,
        ILookup<Guid?, Endpoint> endpointsByFolder)
    {
        return foldersByParent[parentId]
            .Select(f => new FolderNode(
                f.Id,
                f.ServiceId,
                f.ParentFolderId,
                f.Name,
                f.SortOrder,
                BuildFolders(f.Id, foldersByParent, endpointsByFolder),
                MapEndpoints(endpointsByFolder, f.Id)))
            .ToList();
    }

    private static List<EndpointNode> MapEndpoints(
        ILookup<Guid?, Endpoint> endpointsByFolder,
        Guid? folderId)
    {
        return endpointsByFolder[folderId]
            .Select(e => new EndpointNode(e.Id, e.ServiceId, e.FolderId, e.Name, e.Method, e.Path, e.Description, e.SortOrder))
            .ToList();
    }

    /// <summary>
    /// Returns <paramref name="ordered"/> with the item matching <paramref name="id"/> swapped one
    /// step toward the front (<paramref name="up"/>) or back; null when the id is absent, and the
    /// list unchanged at a boundary.
    /// </summary>
    private static List<T>? Swap<T>(IReadOnlyList<T> ordered, Guid id, bool up, Func<T, Guid> idOf)
    {
        var list = ordered.ToList();
        var index = list.FindIndex(x => idOf(x) == id);
        if (index < 0)
        {
            return null;
        }

        var target = up ? index - 1 : index + 1;
        if (target >= 0 && target < list.Count)
        {
            (list[index], list[target]) = (list[target], list[index]);
        }

        return list;
    }

    private Result<T> EnsureOpen<T>() =>
        _session.IsOpen ? Result.Success<T>(default!) : Result.Failure<T>(ServiceCatalogErrors.NoWorkspaceOpen);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
