using ApiTestingStudio.Application.ServiceCatalog;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using FluentAssertions;

namespace ApiTestingStudio.Application.Tests;

public sealed class EndpointCrudServiceTests
{
    private readonly FakeCatalogStore _store = new();
    private readonly FakeWorkspaceSession _session = new();
    private readonly EndpointCrudService _sut;
    private readonly Service _service;

    public EndpointCrudServiceTests()
    {
        var workspaceId = Guid.NewGuid();
        _session.Current = new Workspace { Id = workspaceId, Name = "WS" };
        _service = new Service { WorkspaceId = workspaceId, Name = "Orders" };
        _store.Services.Add(_service);

        _sut = new EndpointCrudService(
            new InMemoryEndpointRepository(_store),
            new InMemoryServiceRepository(_store),
            new InMemoryEndpointFolderRepository(_store),
            _session);
    }

    [Fact]
    public async Task Create_persists_and_orders_endpoints()
    {
        var first = (await _sut.CreateEndpointAsync(_service.Id, null, new EndpointDraft("List", HttpVerb.Get, "/o"))).Value;
        var second = (await _sut.CreateEndpointAsync(_service.Id, null, new EndpointDraft("Create", HttpVerb.Post, "/o"))).Value;

        first.SortOrder.Should().Be(0);
        second.SortOrder.Should().Be(1);
        _store.Endpoints.Should().HaveCount(2);
    }

    [Fact]
    public async Task Create_fails_on_blank_path()
    {
        var result = await _sut.CreateEndpointAsync(_service.Id, null, new EndpointDraft("List", HttpVerb.Get, "  "));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("service_catalog.path_required");
    }

    [Fact]
    public async Task Create_fails_when_service_missing()
    {
        var result = await _sut.CreateEndpointAsync(Guid.NewGuid(), null, new EndpointDraft("List", HttpVerb.Get, "/o"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("service_catalog.service_not_found");
    }

    [Fact]
    public async Task Update_changes_method_and_path()
    {
        var endpoint = (await _sut.CreateEndpointAsync(_service.Id, null, new EndpointDraft("List", HttpVerb.Get, "/o"))).Value;

        var result = await _sut.UpdateEndpointAsync(endpoint.Id, new EndpointDraft("List", HttpVerb.Post, "/o/new", "desc"));

        result.Value.Method.Should().Be(HttpVerb.Post);
        result.Value.Path.Should().Be("/o/new");
        result.Value.Description.Should().Be("desc");
    }

    [Fact]
    public async Task Duplicate_creates_suffixed_copy()
    {
        var endpoint = (await _sut.CreateEndpointAsync(_service.Id, null, new EndpointDraft("List", HttpVerb.Get, "/o"))).Value;

        var copy = (await _sut.DuplicateEndpointAsync(endpoint.Id)).Value;

        copy.Id.Should().NotBe(endpoint.Id);
        copy.Name.Should().Be("List (copy)");
        _store.Endpoints.Should().HaveCount(2);
    }

    [Fact]
    public async Task Delete_removes_endpoint()
    {
        var endpoint = (await _sut.CreateEndpointAsync(_service.Id, null, new EndpointDraft("List", HttpVerb.Get, "/o"))).Value;

        await _sut.DeleteEndpointAsync(endpoint.Id);

        _store.Endpoints.Should().BeEmpty();
    }

    [Fact]
    public async Task Move_reassigns_folder()
    {
        var folder = new EndpointFolder { ServiceId = _service.Id, Name = "v1" };
        _store.Folders.Add(folder);
        var endpoint = (await _sut.CreateEndpointAsync(_service.Id, null, new EndpointDraft("List", HttpVerb.Get, "/o"))).Value;

        var result = await _sut.MoveEndpointAsync(endpoint.Id, folder.Id);

        result.IsSuccess.Should().BeTrue();
        _store.Endpoints.Single().FolderId.Should().Be(folder.Id);
    }

    [Fact]
    public async Task Reorder_moves_endpoint_down()
    {
        var a = (await _sut.CreateEndpointAsync(_service.Id, null, new EndpointDraft("A", HttpVerb.Get, "/a"))).Value;
        var b = (await _sut.CreateEndpointAsync(_service.Id, null, new EndpointDraft("B", HttpVerb.Get, "/b"))).Value;

        await _sut.ReorderEndpointAsync(a.Id, up: false);

        var ordered = _store.Endpoints.OrderBy(e => e.SortOrder).ToList();
        ordered[0].Id.Should().Be(b.Id);
        ordered[1].Id.Should().Be(a.Id);
    }

    [Fact]
    public async Task Operations_fail_when_no_workspace_open()
    {
        _session.Current = null;

        var result = await _sut.CreateEndpointAsync(_service.Id, null, new EndpointDraft("List", HttpVerb.Get, "/o"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("service_catalog.no_workspace");
    }
}
