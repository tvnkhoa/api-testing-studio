using ApiTestingStudio.Application.Import;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Infrastructure.Persistence;
using ApiTestingStudio.Plugin.Abstractions.Importing;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiTestingStudio.Infrastructure.Tests;

public sealed class CatalogMergerTests : TempDirectoryFixture
{
    private static readonly DateTimeOffset Timestamp = new(2026, 7, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly WorkspaceSession _session = new();
    private readonly SqliteStorageProvider _provider;
    private readonly ServiceRepository _services;
    private readonly EndpointRepository _endpoints;
    private readonly CatalogMerger _merger;
    private Guid _workspaceId;

    public CatalogMergerTests()
    {
        _provider = new SqliteStorageProvider(_session, NullLogger<SqliteStorageProvider>.Instance);
        _services = new ServiceRepository(_session);
        _endpoints = new EndpointRepository(_session);
        _merger = new CatalogMerger(_session, NullLogger<CatalogMerger>.Instance);
    }

    [Fact]
    public async Task Merge_fails_when_no_workspace_open()
    {
        var result = await _merger.MergeAsync(ImportResult.Empty, new ImportOptions());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("import.no_workspace");
    }

    [Fact]
    public async Task Merge_creates_new_service_and_endpoints()
    {
        await OpenWorkspaceAsync();
        var import = OneServiceTwoEndpoints();

        var result = await _merger.MergeAsync(import, new ImportOptions());

        result.IsSuccess.Should().BeTrue();
        result.Value.ServicesCreated.Should().Be(1);
        result.Value.EndpointsCreated.Should().Be(2);

        var services = await _services.GetByWorkspaceAsync(_workspaceId);
        var service = services.Should().ContainSingle().Subject;
        service.WorkspaceId.Should().Be(_workspaceId);
        (await _endpoints.GetByServiceAsync(service.Id)).Should().HaveCount(2);
    }

    [Fact]
    public async Task Merge_into_existing_service_skips_duplicates_by_default()
    {
        await OpenWorkspaceAsync();
        await _merger.MergeAsync(OneServiceTwoEndpoints(), new ImportOptions());

        // Re-import the same definition: same service (name+baseurl) and same endpoints (method+path).
        var result = await _merger.MergeAsync(OneServiceTwoEndpoints(), new ImportOptions());

        result.Value.ServicesCreated.Should().Be(0);
        result.Value.EndpointsCreated.Should().Be(0);
        result.Value.EndpointsSkipped.Should().Be(2);

        var services = await _services.GetByWorkspaceAsync(_workspaceId);
        var service = services.Should().ContainSingle().Subject;
        (await _endpoints.GetByServiceAsync(service.Id)).Should().HaveCount(2);
    }

    [Fact]
    public async Task Merge_overwrites_existing_endpoints_when_requested()
    {
        await OpenWorkspaceAsync();
        await _merger.MergeAsync(OneServiceTwoEndpoints(), new ImportOptions());

        var result = await _merger.MergeAsync(OneServiceTwoEndpoints(), new ImportOptions { OverwriteExisting = true });

        result.Value.EndpointsUpdated.Should().Be(2);
        result.Value.EndpointsSkipped.Should().Be(0);
    }

    [Fact]
    public async Task Merge_rolls_back_completely_on_failure()
    {
        await OpenWorkspaceAsync();

        // Two endpoints sharing the same primary key force a failure during SaveChanges. Because the
        // whole merge runs in one transaction, nothing (not even the service) should be committed.
        var service = new Service { Name = "Broken", BaseUrl = "https://x" };
        var sharedId = Guid.NewGuid();
        var import = new ImportResult(
            [service],
            [
                new Endpoint { Id = sharedId, ServiceId = service.Id, Name = "A", Path = "/a" },
                new Endpoint { Id = sharedId, ServiceId = service.Id, Name = "B", Path = "/b" },
            ]);

        var result = await _merger.MergeAsync(import, new ImportOptions());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("import.merge_failed");
        (await _services.GetByWorkspaceAsync(_workspaceId)).Should().BeEmpty();
    }

    private static ImportResult OneServiceTwoEndpoints()
    {
        var service = new Service { Name = "Petstore", BaseUrl = "https://api.pet.com" };
        return new ImportResult(
            [service],
            [
                new Endpoint { ServiceId = service.Id, Name = "List", Method = HttpVerb.Get, Path = "/pets" },
                new Endpoint { ServiceId = service.Id, Name = "Create", Method = HttpVerb.Post, Path = "/pets" },
            ]);
    }

    private async Task OpenWorkspaceAsync()
    {
        var workspace = new Workspace
        {
            Name = "Merge",
            SchemaVersion = Workspace.CurrentSchemaVersion,
            CreatedUtc = Timestamp,
            ModifiedUtc = Timestamp,
        };
        _workspaceId = workspace.Id;
        await _provider.CreateAsync(PathFor("merge.db"), workspace);
    }
}
