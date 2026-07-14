using ApiTestingStudio.Infrastructure.Settings;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiTestingStudio.Infrastructure.Tests;

public sealed class LayoutPersistenceServiceTests : TempDirectoryFixture
{
    private const string SampleLayout = "<LayoutRoot><RootPanel /></LayoutRoot>";

    private LayoutPersistenceService CreateService(string fileName = "dock-layout.xml")
        => new(PathFor(fileName), NullLogger<LayoutPersistenceService>.Instance);

    [Fact]
    public async Task Load_on_missing_store_returns_null()
    {
        var service = CreateService("does-not-exist.xml");

        (await service.LoadLayoutAsync()).Should().BeNull();
    }

    [Fact]
    public async Task Save_then_load_round_trips_the_payload()
    {
        var service = CreateService();

        await service.SaveLayoutAsync(SampleLayout);
        var reloaded = await CreateService().LoadLayoutAsync();

        reloaded.Should().Be(SampleLayout);
    }

    [Fact]
    public async Task Clear_removes_the_saved_layout()
    {
        var service = CreateService();
        await service.SaveLayoutAsync(SampleLayout);

        await service.ClearAsync();

        (await service.LoadLayoutAsync()).Should().BeNull();
    }
}
