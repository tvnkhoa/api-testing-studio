using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiTestingStudio.Infrastructure.Tests;

public sealed class RunStoreTests : TempDirectoryFixture
{
    private static readonly DateTimeOffset When = new(2026, 7, 20, 10, 0, 0, TimeSpan.Zero);

    private readonly WorkspaceSession _session = new();
    private readonly SqliteStorageProvider _provider;
    private readonly RunStore _runs;

    public RunStoreTests()
    {
        _provider = new SqliteStorageProvider(_session, NullLogger<SqliteStorageProvider>.Instance);
        _runs = new RunStore(_session);
    }

    [Fact]
    public async Task Save_then_read_back_run_and_steps()
    {
        var workspaceId = await OpenWorkspaceAsync();
        var run = new Run
        {
            WorkspaceId = workspaceId,
            Source = RunSource.Workflow,
            TargetName = "Checkout",
            Status = RunStatus.Passed,
            StartedUtc = When,
            CompletedUtc = When.AddSeconds(2),
            DurationMs = 2000,
        };
        var parent = new RunStep { RunId = Guid.Empty, Order = 0, Name = "Loop", Kind = "Loop", Status = RunStatus.Passed };
        var child = new RunStep { RunId = Guid.Empty, ParentStepId = parent.Id, Order = 0, Name = "Call", Kind = "Api", Status = RunStatus.Passed, StatusCode = 200 };

        await _runs.SaveAsync(run, [parent, child]);

        var reloaded = await _runs.GetAsync(run.Id);
        reloaded.Should().NotBeNull();
        reloaded!.TargetName.Should().Be("Checkout");
        reloaded.Source.Should().Be(RunSource.Workflow);

        var steps = await _runs.GetStepsAsync(run.Id);
        steps.Should().HaveCount(2);
        steps.Should().OnlyContain(s => s.RunId == run.Id);
        steps.Single(s => s.Name == "Call").StatusCode.Should().Be(200);
        steps.Single(s => s.Name == "Call").ParentStepId.Should().Be(parent.Id);
    }

    [Fact]
    public async Task List_returns_runs_most_recent_first()
    {
        var workspaceId = await OpenWorkspaceAsync();
        var older = new Run { WorkspaceId = workspaceId, TargetName = "old", Status = RunStatus.Passed, StartedUtc = When, CompletedUtc = When };
        var newer = new Run { WorkspaceId = workspaceId, TargetName = "new", Status = RunStatus.Passed, StartedUtc = When.AddMinutes(5), CompletedUtc = When.AddMinutes(5) };

        await _runs.SaveAsync(older, []);
        await _runs.SaveAsync(newer, []);

        var list = await _runs.ListAsync(workspaceId);

        list.Should().HaveCount(2);
        list[0].TargetName.Should().Be("new");
        list[1].TargetName.Should().Be("old");
    }

    private async Task<Guid> OpenWorkspaceAsync()
    {
        var workspace = new Workspace
        {
            Name = "Runs",
            SchemaVersion = Workspace.CurrentSchemaVersion,
            CreatedUtc = When,
            ModifiedUtc = When,
        };
        await _provider.CreateAsync(PathFor("runs.db"), workspace);
        return workspace.Id;
    }
}
