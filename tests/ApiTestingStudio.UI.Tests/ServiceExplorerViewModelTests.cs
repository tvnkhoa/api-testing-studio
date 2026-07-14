using ApiTestingStudio.Application.ServiceCatalog;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.UI.Messaging;
using ApiTestingStudio.UI.ViewModels.Explorer;
using CommunityToolkit.Mvvm.Messaging;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiTestingStudio.UI.Tests;

public sealed class ServiceExplorerViewModelTests
{
    private readonly FakeServiceExplorerService _explorer = new();
    private readonly FakeEndpointCrudService _endpoints = new();
    private readonly FakeExplorerStateService _state = new();
    private readonly WeakReferenceMessenger _messenger = new();
    private readonly FakeDialogService _dialog = new();
    private readonly FakeWorkspaceSession _session = new();
    private readonly FakeStatusBarService _status = new();

    private ServiceExplorerViewModel CreateViewModel()
    {
        _session.Open(new Workspace { Name = "WS" }, @"C:\ws.atsdb");
        return new ServiceExplorerViewModel(
            _explorer, _endpoints, _state, _messenger, _dialog, _session, _status,
            NullLogger<ServiceExplorerViewModel>.Instance);
    }

    private static ServiceCatalogTree TreeWithOneEndpoint(out Guid serviceId, out Guid endpointId)
    {
        serviceId = Guid.NewGuid();
        endpointId = Guid.NewGuid();
        var endpoint = new EndpointNode(endpointId, serviceId, null, "List", HttpVerb.Get, "/orders", null, 0);
        var service = new ServiceNode(serviceId, "Orders", null, null, 0, [], [endpoint]);
        return new ServiceCatalogTree([service]);
    }

    [Fact]
    public async Task Load_populates_roots()
    {
        _explorer.Tree = TreeWithOneEndpoint(out _, out _);
        var vm = CreateViewModel();

        await vm.LoadAsync();

        vm.Roots.Should().ContainSingle().Which.Should().BeOfType<ServiceNodeViewModel>()
            .Which.Children.Should().ContainSingle().Which.Should().BeOfType<EndpointNodeViewModel>();
    }

    [Fact]
    public async Task Load_when_no_workspace_clears_tree()
    {
        _explorer.Tree = TreeWithOneEndpoint(out _, out _);
        var vm = CreateViewModel();
        await vm.LoadAsync();

        _session.Close();
        await vm.LoadAsync();

        vm.Roots.Should().BeEmpty();
    }

    [Fact]
    public async Task Selecting_endpoint_publishes_message()
    {
        _explorer.Tree = TreeWithOneEndpoint(out var serviceId, out var endpointId);
        var vm = CreateViewModel();
        await vm.LoadAsync();

        EndpointSelectedMessage? received = null;
        _messenger.Register<EndpointSelectedMessage>(this, (_, m) => received = m);

        var endpoint = (EndpointNodeViewModel)vm.Roots[0].Children[0];
        endpoint.IsSelected = true;

        received.Should().NotBeNull();
        received!.EndpointId.Should().Be(endpointId);
        received.ServiceId.Should().Be(serviceId);
    }

    [Fact]
    public async Task Search_hides_non_matching_nodes()
    {
        _explorer.Tree = TreeWithOneEndpoint(out _, out _);
        var vm = CreateViewModel();
        await vm.LoadAsync();

        vm.SearchText = "zzz";

        vm.Roots[0].IsVisible.Should().BeFalse();

        vm.SearchText = "list";
        vm.Roots[0].IsVisible.Should().BeTrue();
        vm.Roots[0].Children[0].IsVisible.Should().BeTrue();
    }

    [Fact]
    public async Task Expanding_a_node_persists_state()
    {
        _explorer.Tree = TreeWithOneEndpoint(out _, out _);
        var vm = CreateViewModel();
        await vm.LoadAsync();
        var saveCountBefore = _state.SaveCount;

        vm.Roots[0].IsExpanded = true;

        _state.SaveCount.Should().BeGreaterThan(saveCountBefore);
        _state.State.ExpandedIds.Should().Contain(vm.Roots[0].Id);
    }

    [Fact]
    public async Task Restore_expands_saved_nodes()
    {
        _explorer.Tree = TreeWithOneEndpoint(out var serviceId, out _);
        _state.State = new ExplorerTreeState([serviceId], null);
        var vm = CreateViewModel();

        await vm.LoadAsync();

        vm.Roots[0].IsExpanded.Should().BeTrue();
    }

    [Fact]
    public async Task AddService_uses_dialog_and_calls_service()
    {
        var vm = CreateViewModel();
        await vm.LoadAsync();
        _dialog.ServiceResult = new ServiceDraft("New");

        await vm.AddServiceCommand.ExecuteAsync(null);

        _explorer.CreatedServices.Should().ContainSingle().Which.Name.Should().Be("New");
    }

    [Fact]
    public async Task AddService_cancelled_dialog_does_nothing()
    {
        var vm = CreateViewModel();
        await vm.LoadAsync();
        _dialog.ServiceResult = null;

        await vm.AddServiceCommand.ExecuteAsync(null);

        _explorer.CreatedServices.Should().BeEmpty();
    }

    [Fact]
    public async Task Child_commands_disabled_without_selection_and_enabled_on_service()
    {
        _explorer.Tree = TreeWithOneEndpoint(out _, out _);
        var vm = CreateViewModel();
        await vm.LoadAsync();

        vm.AddFolderCommand.CanExecute(null).Should().BeFalse();
        vm.DuplicateCommand.CanExecute(null).Should().BeFalse();

        vm.Roots[0].IsSelected = true;

        vm.AddFolderCommand.CanExecute(null).Should().BeTrue();
        vm.AddEndpointCommand.CanExecute(null).Should().BeTrue();
        vm.DuplicateCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task Duplicate_enabled_only_for_endpoint_selection()
    {
        _explorer.Tree = TreeWithOneEndpoint(out _, out _);
        var vm = CreateViewModel();
        await vm.LoadAsync();

        vm.Roots[0].Children[0].IsSelected = true;

        vm.DuplicateCommand.CanExecute(null).Should().BeTrue();
    }
}
