using ApiTestingStudio.Application.ServiceCatalog;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using FluentAssertions;

namespace ApiTestingStudio.Application.Tests;

public sealed class ServiceExplorerServiceTests
{
    private readonly FakeCatalogStore _store = new();
    private readonly FakeWorkspaceSession _session = new();
    private readonly ServiceExplorerService _sut;
    private readonly Guid _workspaceId = Guid.NewGuid();

    public ServiceExplorerServiceTests()
    {
        _session.Current = new Workspace { Id = _workspaceId, Name = "WS" };
        _sut = new ServiceExplorerService(
            new InMemoryServiceRepository(_store),
            new InMemoryEndpointFolderRepository(_store),
            new InMemoryEndpointRepository(_store),
            _session);
    }

    [Fact]
    public async Task LoadTree_fails_when_no_workspace_open()
    {
        _session.Current = null;

        var result = await _sut.LoadTreeAsync();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("service_catalog.no_workspace");
    }

    [Fact]
    public async Task LoadTree_builds_nested_hierarchy()
    {
        var service = await CreateServiceAsync("Orders");
        var folder = (await _sut.CreateFolderAsync(service.Id, null, "v1")).Value;
        var sub = (await _sut.CreateFolderAsync(service.Id, folder.Id, "admin")).Value;
        _store.Endpoints.Add(new Endpoint { ServiceId = service.Id, FolderId = folder.Id, Name = "List", Path = "/o" });
        _store.Endpoints.Add(new Endpoint { ServiceId = service.Id, FolderId = sub.Id, Name = "Purge", Path = "/o/purge" });
        _store.Endpoints.Add(new Endpoint { ServiceId = service.Id, Name = "Ping", Path = "/ping" });

        var tree = (await _sut.LoadTreeAsync()).Value;

        var serviceNode = tree.Services.Should().ContainSingle().Subject;
        serviceNode.Endpoints.Should().ContainSingle(e => e.Name == "Ping");
        var v1 = serviceNode.Folders.Should().ContainSingle().Subject;
        v1.Endpoints.Should().ContainSingle(e => e.Name == "List");
        v1.Folders.Should().ContainSingle(f => f.Name == "admin")
            .Which.Endpoints.Should().ContainSingle(e => e.Name == "Purge");
    }

    [Fact]
    public async Task CreateService_assigns_incrementing_sort_order()
    {
        var first = await CreateServiceAsync("A");
        var second = await CreateServiceAsync("B");

        first.SortOrder.Should().Be(0);
        second.SortOrder.Should().Be(1);
    }

    [Fact]
    public async Task CreateService_fails_on_blank_name()
    {
        var result = await _sut.CreateServiceAsync(new ServiceDraft("   "));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("service_catalog.name_required");
    }

    [Fact]
    public async Task DeleteService_removes_service_folders_and_endpoints()
    {
        var service = await CreateServiceAsync("Orders");
        var folder = (await _sut.CreateFolderAsync(service.Id, null, "v1")).Value;
        _store.Endpoints.Add(new Endpoint { ServiceId = service.Id, FolderId = folder.Id, Name = "List", Path = "/o" });

        var result = await _sut.DeleteServiceAsync(service.Id);

        result.IsSuccess.Should().BeTrue();
        _store.Services.Should().BeEmpty();
        _store.Folders.Should().BeEmpty();
        _store.Endpoints.Should().BeEmpty();
    }

    [Fact]
    public async Task ReorderService_moves_item_up()
    {
        var a = await CreateServiceAsync("A");
        var b = await CreateServiceAsync("B");

        await _sut.ReorderServiceAsync(b.Id, up: true);

        var ordered = _store.Services.OrderBy(s => s.SortOrder).ToList();
        ordered[0].Id.Should().Be(b.Id);
        ordered[1].Id.Should().Be(a.Id);
    }

    [Fact]
    public async Task RenameFolder_persists_new_name()
    {
        var service = await CreateServiceAsync("Orders");
        var folder = (await _sut.CreateFolderAsync(service.Id, null, "old")).Value;

        var result = await _sut.RenameFolderAsync(folder.Id, "new");

        result.Value.Name.Should().Be("new");
        _store.Folders.Single().Name.Should().Be("new");
    }

    [Fact]
    public async Task DeleteFolder_cascades_nested_subtree()
    {
        var service = await CreateServiceAsync("Orders");
        var parent = (await _sut.CreateFolderAsync(service.Id, null, "v1")).Value;
        var child = (await _sut.CreateFolderAsync(service.Id, parent.Id, "admin")).Value;
        _store.Endpoints.Add(new Endpoint { ServiceId = service.Id, FolderId = child.Id, Name = "X", Path = "/x", Method = HttpVerb.Delete });

        await _sut.DeleteFolderAsync(parent.Id);

        _store.Folders.Should().BeEmpty();
        _store.Endpoints.Should().BeEmpty();
    }

    private async Task<Service> CreateServiceAsync(string name)
        => (await _sut.CreateServiceAsync(new ServiceDraft(name))).Value;
}
