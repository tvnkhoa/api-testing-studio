using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Tests;

/// <summary>Mutable in-memory <see cref="IWorkspaceSession"/> for exercising catalog services.</summary>
internal sealed class FakeWorkspaceSession : IWorkspaceSession
{
    public bool IsOpen => Current is not null;

    public Workspace? Current { get; set; }

    public string? Location { get; set; }
}

/// <summary>Shared in-memory backing store so cascade deletes span the three catalog repositories.</summary>
internal sealed class FakeCatalogStore
{
    public List<Service> Services { get; } = [];

    public List<EndpointFolder> Folders { get; } = [];

    public List<Endpoint> Endpoints { get; } = [];
}

internal sealed class InMemoryServiceRepository : IServiceRepository
{
    private readonly FakeCatalogStore _store;

    public InMemoryServiceRepository(FakeCatalogStore store) => _store = store;

    public Task<IReadOnlyList<Service>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Service>>(_store.Services
            .Where(s => s.WorkspaceId == workspaceId)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Name)
            .ToList());

    public Task<Service?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.Services.FirstOrDefault(s => s.Id == id));

    public Task AddAsync(Service service, CancellationToken cancellationToken = default)
    {
        _store.Services.Add(service);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Service service, CancellationToken cancellationToken = default)
    {
        var index = _store.Services.FindIndex(s => s.Id == service.Id);
        if (index >= 0)
        {
            _store.Services[index] = service;
        }

        return Task.CompletedTask;
    }

    public Task DeleteCascadeAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        _store.Endpoints.RemoveAll(e => e.ServiceId == serviceId);
        _store.Folders.RemoveAll(f => f.ServiceId == serviceId);
        _store.Services.RemoveAll(s => s.Id == serviceId);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryEndpointFolderRepository : IEndpointFolderRepository
{
    private readonly FakeCatalogStore _store;

    public InMemoryEndpointFolderRepository(FakeCatalogStore store) => _store = store;

    public Task<IReadOnlyList<EndpointFolder>> GetByServiceAsync(Guid serviceId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<EndpointFolder>>(_store.Folders
            .Where(f => f.ServiceId == serviceId)
            .OrderBy(f => f.SortOrder).ThenBy(f => f.Name)
            .ToList());

    public Task<EndpointFolder?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.Folders.FirstOrDefault(f => f.Id == id));

    public Task AddAsync(EndpointFolder folder, CancellationToken cancellationToken = default)
    {
        _store.Folders.Add(folder);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(EndpointFolder folder, CancellationToken cancellationToken = default)
    {
        var index = _store.Folders.FindIndex(f => f.Id == folder.Id);
        if (index >= 0)
        {
            _store.Folders[index] = folder;
        }

        return Task.CompletedTask;
    }

    public Task DeleteCascadeAsync(Guid folderId, CancellationToken cancellationToken = default)
    {
        var folder = _store.Folders.FirstOrDefault(f => f.Id == folderId);
        if (folder is null)
        {
            return Task.CompletedTask;
        }

        var subtree = ResolveSubtree(folderId, _store.Folders.Where(f => f.ServiceId == folder.ServiceId).ToList());
        _store.Endpoints.RemoveAll(e => e.FolderId is { } fid && subtree.Contains(fid));
        _store.Folders.RemoveAll(f => subtree.Contains(f.Id));
        return Task.CompletedTask;
    }

    private static HashSet<Guid> ResolveSubtree(Guid rootId, IReadOnlyList<EndpointFolder> all)
    {
        var result = new HashSet<Guid> { rootId };
        var queue = new Queue<Guid>();
        queue.Enqueue(rootId);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var child in all.Where(f => f.ParentFolderId == current))
            {
                if (result.Add(child.Id))
                {
                    queue.Enqueue(child.Id);
                }
            }
        }

        return result;
    }
}

internal sealed class InMemoryEndpointRepository : IEndpointRepository
{
    private readonly FakeCatalogStore _store;

    public InMemoryEndpointRepository(FakeCatalogStore store) => _store = store;

    public Task<IReadOnlyList<Endpoint>> GetByServiceAsync(Guid serviceId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Endpoint>>(_store.Endpoints
            .Where(e => e.ServiceId == serviceId)
            .OrderBy(e => e.SortOrder).ThenBy(e => e.Name)
            .ToList());

    public Task<Endpoint?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.Endpoints.FirstOrDefault(e => e.Id == id));

    public Task AddAsync(Endpoint endpoint, CancellationToken cancellationToken = default)
    {
        _store.Endpoints.Add(endpoint);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Endpoint endpoint, CancellationToken cancellationToken = default)
    {
        var index = _store.Endpoints.FindIndex(e => e.Id == endpoint.Id);
        if (index >= 0)
        {
            _store.Endpoints[index] = endpoint;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.Endpoints.RemoveAll(e => e.Id == id);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryWorkspaceSettingRepository : IWorkspaceSettingRepository
{
    private readonly Dictionary<string, string?> _values = [];

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
        => Task.FromResult(_values.TryGetValue(key, out var value) ? value : null);

    public Task SetAsync(string key, string? value, CancellationToken cancellationToken = default)
    {
        _values[key] = value;
        return Task.CompletedTask;
    }
}
