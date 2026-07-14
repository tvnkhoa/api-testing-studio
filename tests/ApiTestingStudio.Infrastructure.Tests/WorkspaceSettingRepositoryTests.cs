using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiTestingStudio.Infrastructure.Tests;

public sealed class WorkspaceSettingRepositoryTests : TempDirectoryFixture
{
    private static readonly DateTimeOffset Timestamp = new(2026, 7, 14, 10, 0, 0, TimeSpan.Zero);

    private readonly WorkspaceSession _session = new();
    private readonly SqliteStorageProvider _provider;
    private readonly WorkspaceSettingRepository _repository;

    public WorkspaceSettingRepositoryTests()
    {
        _provider = new SqliteStorageProvider(_session, NullLogger<SqliteStorageProvider>.Instance);
        _repository = new WorkspaceSettingRepository(_session);
    }

    [Fact]
    public async Task Get_returns_null_when_absent()
    {
        await OpenWorkspaceAsync();

        (await _repository.GetAsync("explorer.tree-state")).Should().BeNull();
    }

    [Fact]
    public async Task Set_inserts_then_updates_the_same_key()
    {
        await OpenWorkspaceAsync();

        await _repository.SetAsync("explorer.tree-state", "{\"a\":1}");
        (await _repository.GetAsync("explorer.tree-state")).Should().Be("{\"a\":1}");

        await _repository.SetAsync("explorer.tree-state", "{\"a\":2}");
        (await _repository.GetAsync("explorer.tree-state")).Should().Be("{\"a\":2}");
    }

    private async Task OpenWorkspaceAsync()
    {
        var workspace = new Workspace
        {
            Name = "Settings",
            SchemaVersion = Workspace.CurrentSchemaVersion,
            CreatedUtc = Timestamp,
            ModifiedUtc = Timestamp,
        };

        await _provider.CreateAsync(PathFor("settings.db"), workspace);
    }
}
