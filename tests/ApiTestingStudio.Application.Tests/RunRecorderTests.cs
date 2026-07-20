using ApiTestingStudio.Application.Runs;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using FluentAssertions;

namespace ApiTestingStudio.Application.Tests;

public sealed class RunRecorderTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private readonly InMemoryRunStore _store = new();
    private readonly MetricsFeed _feed = new();
    private readonly FakeWorkspaceSession _session = new() { Current = new Workspace { Id = WorkspaceId, Name = "WS" } };
    private readonly RunRecorder _sut;

    public RunRecorderTests()
    {
        _sut = new RunRecorder(_store, _feed, _session, new FixedClock(Now));
    }

    [Fact]
    public async Task RecordRequest_saves_a_request_run_with_snapshot_step_and_publishes()
    {
        Run? published = null;
        _feed.RunRecorded += (_, run) => published = run;

        var request = new HttpRequestModel { Method = HttpVerb.Post, Url = "https://api.example.com/orders" };
        var execution = new HttpExecutionResult
        {
            Response = new HttpResponseModel { StatusCode = 201, ReasonPhrase = "Created" },
            Timing = new RequestTiming { Total = TimeSpan.FromMilliseconds(42) },
        };
        var endpointId = Guid.NewGuid();

        await _sut.RecordRequestAsync(endpointId, request, execution);

        _store.Runs.Should().ContainSingle();
        var run = _store.Runs[0];
        run.Source.Should().Be(RunSource.Request);
        run.WorkspaceId.Should().Be(WorkspaceId);
        run.TargetId.Should().Be(endpointId);
        run.Status.Should().Be(RunStatus.Passed);
        run.DurationMs.Should().Be(42);

        _store.Steps.Should().ContainSingle();
        var step = _store.Steps[0];
        step.Kind.Should().Be("Request");
        step.StatusCode.Should().Be(201);
        step.RequestSnapshot.Should().NotBeNullOrEmpty();
        step.ResponseSnapshot.Should().NotBeNullOrEmpty();

        published.Should().BeSameAs(run);
    }

    [Fact]
    public async Task RecordRequest_marks_failure_for_error_status()
    {
        var execution = new HttpExecutionResult
        {
            Response = new HttpResponseModel { StatusCode = 500, ReasonPhrase = "Error" },
            Timing = new RequestTiming { Total = TimeSpan.FromMilliseconds(10) },
        };

        await _sut.RecordRequestAsync(Guid.NewGuid(), new HttpRequestModel { Url = "https://x" }, execution);

        _store.Runs[0].Status.Should().Be(RunStatus.Failed);
    }

    [Fact]
    public async Task RecordWorkflow_flattens_the_node_tree_into_nested_steps()
    {
        var workflow = new Workflow { Id = Guid.NewGuid(), WorkspaceId = WorkspaceId, Name = "Checkout", Nodes = [], Edges = [] };
        var result = new WorkflowRunResult
        {
            WorkflowId = workflow.Id,
            Status = RunStatus.Passed,
            Duration = TimeSpan.FromMilliseconds(120),
            Nodes =
            [
                new NodeRunResult
                {
                    NodeName = "Loop",
                    Kind = WorkflowNodeKind.Loop,
                    Status = RunStatus.Passed,
                    Children =
                    [
                        new NodeRunResult { NodeName = "Call", Kind = WorkflowNodeKind.Api, Status = RunStatus.Passed, Iteration = 0 },
                    ],
                },
            ],
        };

        await _sut.RecordWorkflowAsync(workflow, result);

        var run = _store.Runs.Should().ContainSingle().Subject;
        run.Source.Should().Be(RunSource.Workflow);
        run.WorkflowId.Should().Be(workflow.Id);

        _store.Steps.Should().HaveCount(2);
        var parent = _store.Steps.Single(s => s.Name == "Loop");
        var child = _store.Steps.Single(s => s.Name == "Call");
        child.ParentStepId.Should().Be(parent.Id);
        child.Iteration.Should().Be(0);
    }

    [Fact]
    public async Task RecordStress_writes_a_header_and_summary_step()
    {
        var stress = new StressRun
        {
            WorkspaceId = WorkspaceId,
            TargetName = "GET /ping",
            Mode = StressMode.Concurrent,
            StartedUtc = Now,
            CompletedUtc = Now.AddSeconds(5),
            RequestCount = 500,
            RequestsPerSecond = 100,
            P95Ms = 30,
            ErrorRate = 0,
        };

        await _sut.RecordStressAsync(stress);

        var run = _store.Runs.Should().ContainSingle().Subject;
        run.Source.Should().Be(RunSource.Stress);
        run.Status.Should().Be(RunStatus.Passed);
        run.DurationMs.Should().Be(5000);
        _store.Steps.Should().ContainSingle().Which.Kind.Should().Be("Stress");
    }

    [Fact]
    public async Task Recording_is_a_no_op_when_no_workspace_is_open()
    {
        _session.Current = null;

        await _sut.RecordRequestAsync(Guid.NewGuid(), new HttpRequestModel { Url = "https://x" }, new HttpExecutionResult
        {
            Response = new HttpResponseModel { StatusCode = 200, ReasonPhrase = "OK" },
            Timing = new RequestTiming { Total = TimeSpan.Zero },
        });

        _store.Runs.Should().BeEmpty();
    }
}
