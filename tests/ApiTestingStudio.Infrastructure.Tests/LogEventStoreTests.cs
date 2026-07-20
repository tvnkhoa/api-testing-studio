using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiTestingStudio.Infrastructure.Tests;

public sealed class LogEventStoreTests : TempDirectoryFixture
{
    private static readonly DateTimeOffset When = new(2026, 7, 20, 10, 0, 0, TimeSpan.Zero);

    private readonly WorkspaceSession _session = new();
    private readonly SqliteStorageProvider _provider;
    private readonly LogEventStore _logs;

    public LogEventStoreTests()
    {
        _provider = new SqliteStorageProvider(_session, NullLogger<SqliteStorageProvider>.Instance);
        _logs = new LogEventStore(_session);
    }

    [Fact]
    public async Task Append_then_query_returns_most_recent_first_and_filters()
    {
        var workspaceId = await OpenWorkspaceAsync();
        await _logs.AppendAsync(
        [
            Event(workspaceId, "Information", "Startup", "app started", 0),
            Event(workspaceId, "Warning", "Runner", "slow request", 1),
            Event(workspaceId, "Error", "Runner", "request failed", 2),
        ]);

        var all = await _logs.QueryAsync(workspaceId, new LogEventQuery());
        all.Should().HaveCount(3);
        all[0].Message.Should().Be("request failed"); // most recent first

        var errorsOnly = await _logs.QueryAsync(workspaceId, new LogEventQuery { Levels = ["Error", "Fatal"] });
        errorsOnly.Should().ContainSingle().Which.Message.Should().Be("request failed");

        var runnerOnly = await _logs.QueryAsync(workspaceId, new LogEventQuery { Source = "Runner" });
        runnerOnly.Should().HaveCount(2);

        var searched = await _logs.QueryAsync(workspaceId, new LogEventQuery { SearchText = "FAILED" });
        searched.Should().ContainSingle().Which.Message.Should().Be("request failed");
    }

    [Fact]
    public async Task GetSources_returns_distinct_sorted_sources()
    {
        var workspaceId = await OpenWorkspaceAsync();
        await _logs.AppendAsync(
        [
            Event(workspaceId, "Information", "Runner", "a", 0),
            Event(workspaceId, "Information", "Startup", "b", 1),
            Event(workspaceId, "Information", "Runner", "c", 2),
        ]);

        var sources = await _logs.GetSourcesAsync(workspaceId);

        sources.Should().Equal("Runner", "Startup");
    }

    private static LogEventRecord Event(Guid workspaceId, string level, string source, string message, int offsetSeconds) => new()
    {
        WorkspaceId = workspaceId,
        TimestampUtc = When.AddSeconds(offsetSeconds),
        Level = level,
        Source = source,
        Message = message,
    };

    private async Task<Guid> OpenWorkspaceAsync()
    {
        var workspace = new Workspace
        {
            Name = "Logs",
            SchemaVersion = Workspace.CurrentSchemaVersion,
            CreatedUtc = When,
            ModifiedUtc = When,
        };
        await _provider.CreateAsync(PathFor("logs.db"), workspace);
        return workspace.Id;
    }
}
