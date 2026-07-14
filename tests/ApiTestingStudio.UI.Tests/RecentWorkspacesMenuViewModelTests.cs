using ApiTestingStudio.Application.Workspaces;
using ApiTestingStudio.UI.ViewModels;
using FluentAssertions;

namespace ApiTestingStudio.UI.Tests;

public sealed class RecentWorkspacesMenuViewModelTests
{
    private static RecentWorkspaceEntry Entry(string location, string name) => new()
    {
        Location = location,
        Name = name,
        LastOpenedUtc = DateTimeOffset.UnixEpoch,
    };

    [Fact]
    public async Task Refresh_populates_items_and_has_items_flag()
    {
        var service = new FakeRecentWorkspacesService();
        service.Seed(Entry("a.atsdb", "A"), Entry("b.atsdb", "B"));
        var vm = new RecentWorkspacesMenuViewModel(service);

        await vm.RefreshAsync();

        vm.HasItems.Should().BeTrue();
        vm.Items.Should().HaveCount(2);
        vm.Items[0].DisplayText.Should().Contain("A").And.Contain("a.atsdb");
    }

    [Fact]
    public async Task Refresh_with_no_entries_clears_has_items()
    {
        var vm = new RecentWorkspacesMenuViewModel(new FakeRecentWorkspacesService());

        await vm.RefreshAsync();

        vm.HasItems.Should().BeFalse();
        vm.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Selecting_an_item_raises_open_requested_with_its_location()
    {
        var service = new FakeRecentWorkspacesService();
        service.Seed(Entry("a.atsdb", "A"));
        var vm = new RecentWorkspacesMenuViewModel(service);
        await vm.RefreshAsync();

        string? requested = null;
        vm.OpenRequested += (_, location) => requested = location;

        vm.Items[0].OpenCommand.Execute(null);

        requested.Should().Be("a.atsdb");
    }

    [Fact]
    public async Task Removing_an_item_drops_it_from_the_store_and_refreshes()
    {
        var service = new FakeRecentWorkspacesService();
        service.Seed(Entry("a.atsdb", "A"), Entry("b.atsdb", "B"));
        var vm = new RecentWorkspacesMenuViewModel(service);
        await vm.RefreshAsync();

        await vm.Items[0].RemoveCommand.ExecuteAsync(null);

        vm.Items.Should().ContainSingle(i => i.Location == "b.atsdb");
        service.Entries.Should().ContainSingle(e => e.Location == "b.atsdb");
    }
}
