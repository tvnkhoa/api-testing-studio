using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Stress;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Plugin.Abstractions.Runners;
using FluentAssertions;

namespace ApiTestingStudio.Application.Tests;

public sealed class StressOrchestratorTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 20, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private sealed class Harness
    {
        public FakeWorkspaceSession Session { get; } = new()
        {
            Current = new Workspace { Name = "WS", Id = WorkspaceId },
        };

        public FakeCatalogStore Catalog { get; } = new();
        public InMemoryWorkflowRepository Workflows { get; } = new();
        public FakeRequestExecutor Executor { get; } = new();
        public FakeWorkflowEngine WorkflowEngine { get; } = new();
        public InMemoryStressRunStore Store { get; } = new();
        public FakeRunRecorder Recorder { get; } = new();
        public FakeStressRunner Runner { get; } = new();

        public bool IncludeRunner { get; set; } = true;

        public StressOrchestrator Build() => new(
            IncludeRunner ? [Runner] : [],
            Executor,
            new InMemoryEndpointRepository(Catalog),
            new InMemoryServiceRepository(Catalog),
            Workflows,
            WorkflowEngine,
            Store,
            Recorder,
            Session,
            new FixedClock(Now));

        public Endpoint SeedEndpoint()
        {
            var service = new Service { WorkspaceId = WorkspaceId, Name = "Svc", BaseUrl = "https://api.example.com" };
            var endpoint = new Endpoint { ServiceId = service.Id, Name = "Ping", Method = HttpVerb.Get, Path = "/ping" };
            Catalog.Services.Add(service);
            Catalog.Endpoints.Add(endpoint);
            return endpoint;
        }
    }

    [Fact]
    public async Task Endpoint_target_builds_the_request_and_persists_the_run()
    {
        var h = new Harness();
        var endpoint = h.SeedEndpoint();
        var request = new StressRunRequest
        {
            Config = new StressRunConfig { Mode = StressMode.Loop, Iterations = 3 },
            TargetKind = StressTargetKind.Endpoint,
            EndpointId = endpoint.Id,
        };

        var result = await h.Build().RunAsync(request);

        result.IsSuccess.Should().BeTrue();
        h.Executor.CallCount.Should().BeGreaterThan(0);
        h.Executor.LastRequest!.Url.Should().Be("https://api.example.com/ping");

        var run = result.Value;
        run.TargetKind.Should().Be(StressTargetKind.Endpoint);
        run.TargetId.Should().Be(endpoint.Id);
        run.TargetName.Should().Be("Ping"); // orchestrator falls back to endpoint.Name when no label is supplied
        run.WorkspaceId.Should().Be(WorkspaceId);
        run.StartedUtc.Should().Be(Now);

        h.Store.Runs.Should().ContainSingle().Which.Id.Should().Be(run.Id);
        h.Store.Metrics.Should().ContainSingle().Which.StressRunId.Should().Be(run.Id);
    }

    [Fact]
    public async Task Workflow_target_drives_the_engine()
    {
        var h = new Harness();
        var workflow = new Workflow { Id = Guid.NewGuid(), WorkspaceId = WorkspaceId, Name = "Flow" };
        h.Workflows.Workflows.Add(workflow);
        var request = new StressRunRequest
        {
            Config = new StressRunConfig { Mode = StressMode.Sequential },
            TargetKind = StressTargetKind.Workflow,
            WorkflowId = workflow.Id,
        };

        var result = await h.Build().RunAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.TargetKind.Should().Be(StressTargetKind.Workflow);
        result.Value.TargetName.Should().Be("Flow");
        h.Runner.WorkloadInvocations.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Missing_endpoint_fails_without_persisting()
    {
        var h = new Harness();
        var request = new StressRunRequest
        {
            Config = new StressRunConfig(),
            TargetKind = StressTargetKind.Endpoint,
            EndpointId = Guid.NewGuid(),
        };

        var result = await h.Build().RunAsync(request);

        result.IsFailure.Should().BeTrue();
        h.Store.Runs.Should().BeEmpty();
    }

    [Fact]
    public async Task No_runner_plugin_yields_runner_unavailable()
    {
        var h = new Harness { IncludeRunner = false };
        var endpoint = h.SeedEndpoint();
        var request = new StressRunRequest
        {
            Config = new StressRunConfig(),
            TargetKind = StressTargetKind.Endpoint,
            EndpointId = endpoint.Id,
        };

        var result = await h.Build().RunAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("stress.runner_unavailable");
    }

    [Fact]
    public async Task No_workspace_open_fails()
    {
        var h = new Harness();
        h.Session.Current = null;
        var request = new StressRunRequest
        {
            Config = new StressRunConfig(),
            TargetKind = StressTargetKind.Endpoint,
            EndpointId = Guid.NewGuid(),
        };

        var result = await h.Build().RunAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("stress.no_workspace");
    }
}

/// <summary>Fake <see cref="IStressRunner"/> that invokes the workload once and returns a fixed result.</summary>
internal sealed class FakeStressRunner : IStressRunner
{
    public int WorkloadInvocations { get; private set; }

    public StressRunConfig? LastConfig { get; private set; }

    public async Task<StressRunResult> RunAsync(
        StressRunConfig config,
        Func<CancellationToken, Task<StressSample>> workload,
        IProgress<StressMetricsSnapshot>? progress = null,
        CancellationToken cancellationToken = default)
    {
        LastConfig = config;
        var sample = await workload(cancellationToken).ConfigureAwait(false);
        WorkloadInvocations++;

        var metrics = new StressMetricsSnapshot
        {
            Elapsed = TimeSpan.FromMilliseconds(100),
            Completed = 1,
            Failed = sample.Success ? 0 : 1,
            RequestsPerSecond = 10,
            MinMs = sample.ElapsedMs,
            AverageMs = sample.ElapsedMs,
            MaxMs = sample.ElapsedMs,
            P50Ms = sample.ElapsedMs,
            P95Ms = sample.ElapsedMs,
            P99Ms = sample.ElapsedMs,
            ErrorRate = sample.Success ? 0 : 1,
        };

        progress?.Report(metrics);
        return new StressRunResult { Config = config, Metrics = metrics, Cancelled = false };
    }
}

/// <summary>In-memory <see cref="IStressRunStore"/> capturing saved runs and metrics.</summary>
internal sealed class InMemoryStressRunStore : IStressRunStore
{
    public List<StressRun> Runs { get; } = [];

    public List<StressMetrics> Metrics { get; } = [];

    public Task SaveAsync(StressRun run, IReadOnlyList<StressMetrics> metrics, CancellationToken cancellationToken = default)
    {
        Runs.Add(run);
        Metrics.AddRange(metrics);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<StressRun>> ListAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<StressRun>>(Runs.Where(r => r.WorkspaceId == workspaceId).ToList());

    public Task<IReadOnlyList<StressMetrics>> GetMetricsAsync(Guid stressRunId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<StressMetrics>>(Metrics.Where(m => m.StressRunId == stressRunId).ToList());
}
