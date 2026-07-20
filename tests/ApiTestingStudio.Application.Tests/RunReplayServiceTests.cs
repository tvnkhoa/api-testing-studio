using ApiTestingStudio.Application.Runs;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using FluentAssertions;

namespace ApiTestingStudio.Application.Tests;

public sealed class RunReplayServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 20, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private readonly InMemoryRunStore _store = new();
    private readonly FakeRequestExecutor _executor = new();
    private readonly InMemoryWorkflowRepository _workflows = new();
    private readonly FakeWorkflowEngine _engine = new();
    private readonly FakeWorkspaceSession _session = new() { Current = new Workspace { Id = WorkspaceId, Name = "WS" } };
    private readonly RunReplayService _sut;

    public RunReplayServiceTests()
    {
        _sut = new RunReplayService(_store, _executor, _workflows, _engine, _session);
    }

    [Fact]
    public async Task Fails_when_no_workspace_open()
    {
        _session.Current = null;

        var result = await _sut.ReplayAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("run.no_workspace");
    }

    [Fact]
    public async Task Fails_when_run_not_found()
    {
        var result = await _sut.ReplayAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("run.not_found");
    }

    [Fact]
    public async Task Request_run_re_executes_the_captured_request()
    {
        // Seed a request run (with a serialized snapshot) via the recorder.
        var recorder = new RunRecorder(_store, new MetricsFeed(), _session, new FixedClock(Now));
        await recorder.RecordRequestAsync(
            Guid.NewGuid(),
            new HttpRequestModel { Method = HttpVerb.Get, Url = "https://api.example.com/ping" },
            new HttpExecutionResult
            {
                Response = new HttpResponseModel { StatusCode = 200, ReasonPhrase = "OK" },
                Timing = new RequestTiming { Total = TimeSpan.FromMilliseconds(10) },
            });
        var runId = _store.Runs[0].Id;

        var result = await _sut.ReplayAsync(runId);

        result.IsSuccess.Should().BeTrue();
        result.Value.StepCount.Should().Be(1);
        _executor.CallCount.Should().Be(1);
        _executor.LastRequest!.Url.Should().Be("https://api.example.com/ping");
    }

    [Fact]
    public async Task Workflow_run_re_runs_the_workflow()
    {
        var workflow = new Workflow { Id = Guid.NewGuid(), WorkspaceId = WorkspaceId, Name = "WF", Nodes = [], Edges = [] };
        _workflows.Workflows.Add(workflow);
        _engine.ResultToReturn = new WorkflowRunResult { WorkflowId = workflow.Id, Status = RunStatus.Passed, Nodes = [] };

        await _store.SaveAsync(
            new Run { WorkspaceId = WorkspaceId, Source = RunSource.Workflow, WorkflowId = workflow.Id, TargetName = "WF", StartedUtc = Now, CompletedUtc = Now },
            []);
        var runId = _store.Runs[0].Id;

        var result = await _sut.ReplayAsync(runId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(RunStatus.Passed);
    }

    [Fact]
    public async Task Stress_run_is_not_replayable()
    {
        await _store.SaveAsync(
            new Run { WorkspaceId = WorkspaceId, Source = RunSource.Stress, TargetName = "load", StartedUtc = Now, CompletedUtc = Now },
            []);
        var runId = _store.Runs[0].Id;

        var result = await _sut.ReplayAsync(runId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("run.stress_replay_unsupported");
    }
}
