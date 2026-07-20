using ApiTestingStudio.Application.Dashboard;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using FluentAssertions;

namespace ApiTestingStudio.Application.Tests;

public sealed class DashboardServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 20, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private readonly InMemoryRunStore _store = new();
    private readonly FakeWorkspaceSession _session = new() { Current = new Workspace { Id = WorkspaceId, Name = "WS" } };
    private readonly DashboardService _sut;

    public DashboardServiceTests()
    {
        _sut = new DashboardService(_store, _session);
    }

    private void Seed(RunStatus status, string target, long durationMs, int index)
        => _store.Runs.Add(new Run
        {
            WorkspaceId = WorkspaceId,
            Source = RunSource.Request,
            TargetName = target,
            Status = status,
            StartedUtc = Now.AddMinutes(index),
            CompletedUtc = Now.AddMinutes(index),
            DurationMs = durationMs,
        });

    [Fact]
    public async Task Empty_store_returns_the_empty_snapshot()
    {
        var result = await _sut.GetSnapshotAsync(new DashboardQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.RunCount.Should().Be(0);
    }

    [Fact]
    public async Task Fails_when_no_workspace_open()
    {
        _session.Current = null;

        var result = await _sut.GetSnapshotAsync(new DashboardQuery());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("run.no_workspace");
    }

    [Fact]
    public async Task Aggregates_counts_rates_rankings_and_status_distribution()
    {
        Seed(RunStatus.Passed, "GET /a", 100, 0);
        Seed(RunStatus.Passed, "GET /a", 200, 1);
        Seed(RunStatus.Failed, "GET /b", 900, 2);
        Seed(RunStatus.Cancelled, "GET /c", 50, 3);

        var result = await _sut.GetSnapshotAsync(new DashboardQuery());

        var snapshot = result.Value;
        snapshot.RunCount.Should().Be(4);
        snapshot.PassedCount.Should().Be(2);
        snapshot.FailedCount.Should().Be(1);
        snapshot.CancelledCount.Should().Be(1);
        snapshot.SuccessRate.Should().BeApproximately(0.5, 0.001);
        snapshot.AverageDurationMs.Should().BeApproximately((100 + 200 + 900 + 50) / 4.0, 0.001);

        // Slowest by average latency: GET /b (900) first.
        snapshot.SlowestTargets[0].Name.Should().Be("GET /b");
        // Most-called: GET /a has 2 runs.
        snapshot.MostCalledTargets[0].Name.Should().Be("GET /a");
        snapshot.MostCalledTargets[0].Count.Should().Be(2);

        snapshot.StatusDistribution.Should().Contain(b => b.Label == "Passed" && b.Count == 2);
        snapshot.StatusDistribution.Should().Contain(b => b.Label == "Failed" && b.Count == 1);
        snapshot.StatusDistribution.Should().Contain(b => b.Label == "Cancelled" && b.Count == 1);

        snapshot.Timeline.Should().HaveCount(4);
    }

    [Fact]
    public async Task Source_filter_restricts_the_aggregate()
    {
        _store.Runs.Add(new Run { WorkspaceId = WorkspaceId, Source = RunSource.Request, TargetName = "r", Status = RunStatus.Passed, StartedUtc = Now, CompletedUtc = Now });
        _store.Runs.Add(new Run { WorkspaceId = WorkspaceId, Source = RunSource.Stress, TargetName = "s", Status = RunStatus.Passed, StartedUtc = Now, CompletedUtc = Now });

        var result = await _sut.GetSnapshotAsync(new DashboardQuery { Source = RunSource.Stress });

        result.Value.RunCount.Should().Be(1);
    }
}
