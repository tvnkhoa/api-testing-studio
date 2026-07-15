using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiTestingStudio.Infrastructure.Tests;

public sealed class RequestHistoryRepositoryTests : TempDirectoryFixture
{
    private static readonly DateTimeOffset Timestamp = new(2026, 7, 14, 10, 0, 0, TimeSpan.Zero);

    private readonly WorkspaceSession _session = new();
    private readonly SqliteStorageProvider _provider;
    private readonly RequestHistoryRepository _history;

    public RequestHistoryRepositoryTests()
    {
        _provider = new SqliteStorageProvider(_session, NullLogger<SqliteStorageProvider>.Instance);
        _history = new RequestHistoryRepository(_session);
    }

    [Fact]
    public async Task Add_then_read_back_by_endpoint_most_recent_first()
    {
        await OpenWorkspaceAsync();
        var endpointId = Guid.NewGuid();

        await _history.AddAsync(NewEntry(endpointId, "https://api.example.com/1", Timestamp));
        await _history.AddAsync(NewEntry(endpointId, "https://api.example.com/2", Timestamp.AddSeconds(5)));
        await _history.AddAsync(NewEntry(Guid.NewGuid(), "https://api.example.com/other", Timestamp));

        var entries = await _history.GetByEndpointAsync(endpointId);

        entries.Should().HaveCount(2);
        entries[0].Url.Should().Be("https://api.example.com/2");
        entries[1].Url.Should().Be("https://api.example.com/1");
    }

    [Fact]
    public async Task Entry_round_trips_all_fields()
    {
        await OpenWorkspaceAsync();
        var entry = NewEntry(Guid.NewGuid(), "https://api.example.com/orders", Timestamp) with
        {
            Method = HttpVerb.Post,
            StatusCode = 201,
            TotalMs = 123,
            DnsMs = 7,
            ConnectMs = 12,
            TimeToFirstByteMs = 80,
            RequestSnapshot = "{\"method\":\"Post\"}",
            ResponseSnapshot = "{\"statusCode\":201}",
        };
        await _history.AddAsync(entry);

        var reloaded = await _history.GetAsync(entry.Id);

        reloaded.Should().NotBeNull();
        reloaded!.Method.Should().Be(HttpVerb.Post);
        reloaded.StatusCode.Should().Be(201);
        reloaded.TotalMs.Should().Be(123);
        reloaded.DnsMs.Should().Be(7);
        reloaded.RequestSnapshot.Should().Be("{\"method\":\"Post\"}");
        reloaded.TimestampUtc.Should().Be(Timestamp);
    }

    [Fact]
    public async Task DeleteByEndpoint_removes_only_that_endpoints_history()
    {
        await OpenWorkspaceAsync();
        var keep = Guid.NewGuid();
        var drop = Guid.NewGuid();
        await _history.AddAsync(NewEntry(keep, "https://api.example.com/keep", Timestamp));
        await _history.AddAsync(NewEntry(drop, "https://api.example.com/drop", Timestamp));

        await _history.DeleteByEndpointAsync(drop);

        (await _history.GetByEndpointAsync(drop)).Should().BeEmpty();
        (await _history.GetByEndpointAsync(keep)).Should().ContainSingle();
    }

    private async Task OpenWorkspaceAsync()
    {
        var workspace = new Workspace
        {
            Name = "Runner",
            SchemaVersion = Workspace.CurrentSchemaVersion,
            CreatedUtc = Timestamp,
            ModifiedUtc = Timestamp,
        };

        await _provider.CreateAsync(PathFor("runner.db"), workspace);
    }

    private static RequestHistoryEntry NewEntry(Guid endpointId, string url, DateTimeOffset timestamp) => new()
    {
        EndpointId = endpointId,
        Method = HttpVerb.Get,
        Url = url,
        StatusCode = 200,
        TotalMs = 10,
        RequestSnapshot = "{}",
        ResponseSnapshot = "{}",
        TimestampUtc = timestamp,
    };
}
