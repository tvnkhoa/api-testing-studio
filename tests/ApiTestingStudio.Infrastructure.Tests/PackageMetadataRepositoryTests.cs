using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiTestingStudio.Infrastructure.Tests;

public sealed class PackageMetadataRepositoryTests : TempDirectoryFixture
{
    private static readonly DateTimeOffset Timestamp = new(2026, 7, 14, 10, 0, 0, TimeSpan.Zero);

    private readonly WorkspaceSession _session = new();
    private readonly SqliteStorageProvider _provider;
    private readonly PackageMetadataRepository _repository;

    public PackageMetadataRepositoryTests()
    {
        _provider = new SqliteStorageProvider(_session, NullLogger<SqliteStorageProvider>.Instance);
        _repository = new PackageMetadataRepository(_session);
    }

    [Fact]
    public async Task Upsert_inserts_then_reads_back()
    {
        await OpenWorkspaceAsync();

        await _repository.UpsertAsync(NewPackage("import.curl", "cURL Importer", "1.0.0"));

        var all = await _repository.GetAllAsync();
        all.Should().ContainSingle();
        all[0].PluginId.Should().Be("import.curl");
        all[0].Version.Should().Be("1.0.0");
    }

    [Fact]
    public async Task Upsert_updates_the_existing_row_by_plugin_id()
    {
        await OpenWorkspaceAsync();
        await _repository.UpsertAsync(NewPackage("import.curl", "cURL Importer", "1.0.0"));

        await _repository.UpsertAsync(NewPackage("import.curl", "cURL Importer", "2.0.0"));

        var all = await _repository.GetAllAsync();
        all.Should().ContainSingle();
        all[0].Version.Should().Be("2.0.0");
    }

    [Fact]
    public async Task Remove_deletes_the_record()
    {
        await OpenWorkspaceAsync();
        await _repository.UpsertAsync(NewPackage("import.curl", "cURL Importer", "1.0.0"));

        await _repository.RemoveAsync("import.curl");

        (await _repository.GetAllAsync()).Should().BeEmpty();
    }

    private async Task OpenWorkspaceAsync()
    {
        var workspace = new Workspace
        {
            Name = "Pkg",
            SchemaVersion = Workspace.CurrentSchemaVersion,
            CreatedUtc = Timestamp,
            ModifiedUtc = Timestamp,
        };

        await _provider.CreateAsync(PathFor("packages.db"), workspace);
    }

    private static PackageMetadata NewPackage(string pluginId, string name, string version) => new()
    {
        WorkspaceId = Guid.NewGuid(),
        PluginId = pluginId,
        PluginName = name,
        Version = version,
        InstalledUtc = Timestamp,
    };
}
