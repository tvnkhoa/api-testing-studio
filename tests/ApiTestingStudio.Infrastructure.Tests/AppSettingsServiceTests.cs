using System.IO;
using ApiTestingStudio.Application.Settings;
using ApiTestingStudio.Infrastructure.Settings;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiTestingStudio.Infrastructure.Tests;

public sealed class AppSettingsServiceTests : TempDirectoryFixture
{
    private AppSettingsService CreateService(string fileName = "app-settings.json")
        => new(PathFor(fileName), NullLogger<AppSettingsService>.Instance);

    [Fact]
    public async Task Load_on_missing_store_returns_defaults()
    {
        var service = CreateService("does-not-exist.json");

        var settings = await service.LoadAsync();

        settings.Theme.Should().Be(ThemeMode.Light);
    }

    [Fact]
    public async Task Save_then_load_round_trips_the_theme()
    {
        var service = CreateService();

        await service.SaveAsync(new AppSettings { Theme = ThemeMode.Dark });
        var reloaded = await CreateService().LoadAsync();

        reloaded.Theme.Should().Be(ThemeMode.Dark);
    }

    [Fact]
    public async Task Load_on_corrupt_store_returns_defaults()
    {
        var path = PathFor("corrupt.json");
        await File.WriteAllTextAsync(path, "{ this is not valid json");
        var service = new AppSettingsService(path, NullLogger<AppSettingsService>.Instance);

        var settings = await service.LoadAsync();

        settings.Theme.Should().Be(ThemeMode.Light);
    }
}
