using ApiTestingStudio.Application.ServiceCatalog;
using FluentAssertions;

namespace ApiTestingStudio.Application.Tests;

public sealed class ServiceExplorerStateServiceTests
{
    private readonly InMemoryWorkspaceSettingRepository _settings = new();
    private readonly ServiceExplorerStateService _sut;

    public ServiceExplorerStateServiceTests() => _sut = new ServiceExplorerStateService(_settings);

    [Fact]
    public async Task Load_returns_empty_when_absent()
    {
        var state = await _sut.LoadAsync();

        state.ExpandedIds.Should().BeEmpty();
        state.SelectedId.Should().BeNull();
    }

    [Fact]
    public async Task Save_then_load_round_trips()
    {
        var expanded = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var selected = Guid.NewGuid();

        await _sut.SaveAsync(new ExplorerTreeState(expanded, selected));
        var state = await _sut.LoadAsync();

        state.ExpandedIds.Should().BeEquivalentTo(expanded);
        state.SelectedId.Should().Be(selected);
    }

    [Fact]
    public async Task Load_returns_empty_on_corrupt_json()
    {
        await _settings.SetAsync(ServiceExplorerStateService.StateKey, "{ not valid json ");

        var state = await _sut.LoadAsync();

        state.Should().Be(ExplorerTreeState.Empty);
    }
}

public sealed class ServiceCatalogSearchTests
{
    [Theory]
    [InlineData("Orders", "", true)]
    [InlineData("Orders", "   ", true)]
    [InlineData("Orders", "ord", true)]
    [InlineData("Orders", "ORD", true)]
    [InlineData("Orders", "xyz", false)]
    public void Matches_is_case_insensitive_and_match_all_on_empty(string text, string query, bool expected)
        => ServiceCatalogSearch.Matches(text, query).Should().Be(expected);
}
