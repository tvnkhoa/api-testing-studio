using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Workspaces;
using ApiTestingStudio.Infrastructure.Settings;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiTestingStudio.Infrastructure.Tests;

public sealed class RecentWorkspacesServiceTests : TempDirectoryFixture
{
    private static readonly DateTimeOffset Base = new(2026, 7, 14, 10, 0, 0, TimeSpan.Zero);

    private RecentWorkspacesService CreateService(string fileName = "recent.json")
        => new(PathFor(fileName), NullLogger<RecentWorkspacesService>.Instance);

    [Fact]
    public async Task Add_or_touch_orders_most_recent_first()
    {
        var service = CreateService();

        await service.AddOrTouchAsync(Entry("a.db", "A", 0));
        await service.AddOrTouchAsync(Entry("b.db", "B", 1));
        await service.AddOrTouchAsync(Entry("c.db", "C", 2));

        var all = await service.GetAllAsync();
        all.Select(e => e.Name).Should().ContainInOrder("C", "B", "A");
    }

    [Fact]
    public async Task Touching_an_existing_location_moves_it_to_the_top_without_duplicating()
    {
        var service = CreateService();
        await service.AddOrTouchAsync(Entry("a.db", "A", 0));
        await service.AddOrTouchAsync(Entry("b.db", "B", 1));

        await service.AddOrTouchAsync(Entry("a.db", "A", 2));

        var all = await service.GetAllAsync();
        all.Should().HaveCount(2);
        all[0].Location.Should().Be("a.db");
    }

    [Fact]
    public async Task List_is_capped_at_capacity()
    {
        var service = CreateService();
        for (var i = 0; i < IRecentWorkspacesService.Capacity + 5; i++)
        {
            await service.AddOrTouchAsync(Entry($"ws{i}.db", $"W{i}", i));
        }

        var all = await service.GetAllAsync();
        all.Should().HaveCount(IRecentWorkspacesService.Capacity);
        all.Should().NotContain(e => e.Location == "ws0.db");
    }

    [Fact]
    public async Task Entries_survive_reload_from_a_new_instance()
    {
        var writer = CreateService();
        await writer.AddOrTouchAsync(Entry("persist.db", "Persist", 0));

        var reader = CreateService();
        var all = await reader.GetAllAsync();

        all.Should().ContainSingle(e => e.Location == "persist.db");
    }

    [Fact]
    public async Task Remove_drops_the_entry()
    {
        var service = CreateService();
        await service.AddOrTouchAsync(Entry("a.db", "A", 0));
        await service.AddOrTouchAsync(Entry("b.db", "B", 1));

        await service.RemoveAsync("a.db");

        var all = await service.GetAllAsync();
        all.Should().ContainSingle(e => e.Location == "b.db");
    }

    [Fact]
    public async Task Get_all_on_missing_store_returns_empty()
    {
        var service = CreateService("does-not-exist.json");

        (await service.GetAllAsync()).Should().BeEmpty();
    }

    private static RecentWorkspaceEntry Entry(string location, string name, int minutesOffset) => new()
    {
        Location = location,
        Name = name,
        LastOpenedUtc = Base.AddMinutes(minutesOffset),
    };
}
