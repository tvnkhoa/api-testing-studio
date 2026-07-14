using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiTestingStudio.Infrastructure.Tests;

public sealed class ServiceCatalogRepositoryTests : TempDirectoryFixture
{
    private static readonly DateTimeOffset Timestamp = new(2026, 7, 14, 10, 0, 0, TimeSpan.Zero);

    private readonly WorkspaceSession _session = new();
    private readonly SqliteStorageProvider _provider;
    private readonly ServiceRepository _services;
    private readonly EndpointFolderRepository _folders;
    private readonly EndpointRepository _endpoints;
    private Guid _workspaceId;

    public ServiceCatalogRepositoryTests()
    {
        _provider = new SqliteStorageProvider(_session, NullLogger<SqliteStorageProvider>.Instance);
        _services = new ServiceRepository(_session);
        _folders = new EndpointFolderRepository(_session);
        _endpoints = new EndpointRepository(_session);
    }

    [Fact]
    public async Task Service_add_then_read_back_ordered()
    {
        await OpenWorkspaceAsync();

        await _services.AddAsync(NewService("Billing", sortOrder: 1));
        await _services.AddAsync(NewService("Accounts", sortOrder: 0));

        var all = await _services.GetByWorkspaceAsync(_workspaceId);
        all.Should().HaveCount(2);
        all[0].Name.Should().Be("Accounts");
        all[1].Name.Should().Be("Billing");
    }

    [Fact]
    public async Task Service_update_persists_changes()
    {
        await OpenWorkspaceAsync();
        var service = NewService("Orders");
        await _services.AddAsync(service);

        await _services.UpdateAsync(service with { Name = "Orders v2", BaseUrl = "https://api.example.com" });

        var reloaded = await _services.GetAsync(service.Id);
        reloaded!.Name.Should().Be("Orders v2");
        reloaded.BaseUrl.Should().Be("https://api.example.com");
    }

    [Fact]
    public async Task Endpoint_crud_round_trips()
    {
        await OpenWorkspaceAsync();
        var service = NewService("Orders");
        await _services.AddAsync(service);

        var endpoint = new Endpoint { ServiceId = service.Id, Name = "List", Method = HttpVerb.Get, Path = "/orders" };
        await _endpoints.AddAsync(endpoint);

        var byService = await _endpoints.GetByServiceAsync(service.Id);
        byService.Should().ContainSingle();

        await _endpoints.UpdateAsync(endpoint with { Method = HttpVerb.Post, Path = "/orders/create" });
        var reloaded = await _endpoints.GetAsync(endpoint.Id);
        reloaded!.Method.Should().Be(HttpVerb.Post);
        reloaded.Path.Should().Be("/orders/create");

        await _endpoints.DeleteAsync(endpoint.Id);
        (await _endpoints.GetByServiceAsync(service.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteService_cascades_to_folders_and_endpoints()
    {
        await OpenWorkspaceAsync();
        var service = NewService("Orders");
        await _services.AddAsync(service);

        var folder = new EndpointFolder { ServiceId = service.Id, Name = "v1" };
        await _folders.AddAsync(folder);
        await _endpoints.AddAsync(new Endpoint { ServiceId = service.Id, FolderId = folder.Id, Name = "List", Path = "/orders" });
        await _endpoints.AddAsync(new Endpoint { ServiceId = service.Id, Name = "Root", Path = "/" });

        await _services.DeleteCascadeAsync(service.Id);

        (await _services.GetAsync(service.Id)).Should().BeNull();
        (await _folders.GetByServiceAsync(service.Id)).Should().BeEmpty();
        (await _endpoints.GetByServiceAsync(service.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteFolder_cascades_nested_folders_and_their_endpoints()
    {
        await OpenWorkspaceAsync();
        var service = NewService("Orders");
        await _services.AddAsync(service);

        var parent = new EndpointFolder { ServiceId = service.Id, Name = "v1" };
        var child = new EndpointFolder { ServiceId = service.Id, ParentFolderId = parent.Id, Name = "admin" };
        var sibling = new EndpointFolder { ServiceId = service.Id, Name = "v2" };
        await _folders.AddAsync(parent);
        await _folders.AddAsync(child);
        await _folders.AddAsync(sibling);

        await _endpoints.AddAsync(new Endpoint { ServiceId = service.Id, FolderId = parent.Id, Name = "InParent", Path = "/p" });
        await _endpoints.AddAsync(new Endpoint { ServiceId = service.Id, FolderId = child.Id, Name = "InChild", Path = "/c" });
        await _endpoints.AddAsync(new Endpoint { ServiceId = service.Id, FolderId = sibling.Id, Name = "InSibling", Path = "/s" });

        await _folders.DeleteCascadeAsync(parent.Id);

        var remainingFolders = await _folders.GetByServiceAsync(service.Id);
        remainingFolders.Should().ContainSingle().Which.Id.Should().Be(sibling.Id);

        var remainingEndpoints = await _endpoints.GetByServiceAsync(service.Id);
        remainingEndpoints.Should().ContainSingle().Which.Name.Should().Be("InSibling");
    }

    private async Task OpenWorkspaceAsync()
    {
        var workspace = new Workspace
        {
            Name = "Catalog",
            SchemaVersion = Workspace.CurrentSchemaVersion,
            CreatedUtc = Timestamp,
            ModifiedUtc = Timestamp,
        };
        _workspaceId = workspace.Id;

        await _provider.CreateAsync(PathFor("catalog.db"), workspace);
    }

    private Service NewService(string name, int sortOrder = 0) => new()
    {
        WorkspaceId = _workspaceId,
        Name = name,
        SortOrder = sortOrder,
    };
}
