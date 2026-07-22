using System.Collections.Concurrent;
using System.Text.Json;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Profiles;
using ApiTestingStudio.Application.Variables;
using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Application.Workflows.Handlers;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.Tests;

/// <summary>
/// Scriptable <see cref="INodeHandler"/> for engine tests: records which nodes ran, tracks peak
/// concurrency, can fail per node, and can run custom logic (e.g. to trigger cancellation).
/// </summary>
internal sealed class FakeNodeHandler : INodeHandler
{
    private int _current;
    private readonly object _gate = new();

    public FakeNodeHandler(WorkflowNodeKind kind) => Kind = kind;

    public WorkflowNodeKind Kind { get; }

    public ConcurrentBag<string> CalledNodes { get; } = [];

    public int CallCount => CalledNodes.Count;

    public int MaxConcurrency { get; private set; }

    public TimeSpan Delay { get; set; } = TimeSpan.Zero;

    public Dictionary<string, RunStatus> StatusByNode { get; } = [];

    public Func<NodeHandlerContext, CancellationToken, Task>? OnExecute { get; set; }

    public async Task<NodeRunResult> ExecuteAsync(NodeHandlerContext context, CancellationToken cancellationToken = default)
    {
        CalledNodes.Add(context.Node.Name);

        var now = Interlocked.Increment(ref _current);
        lock (_gate)
        {
            if (now > MaxConcurrency)
            {
                MaxConcurrency = now;
            }
        }

        try
        {
            if (OnExecute is not null)
            {
                await OnExecute(context, cancellationToken).ConfigureAwait(false);
            }

            if (Delay > TimeSpan.Zero)
            {
                await Task.Delay(Delay, cancellationToken).ConfigureAwait(false);
            }

            var status = StatusByNode.TryGetValue(context.Node.Name, out var s) ? s : RunStatus.Passed;
            return new NodeRunResult
            {
                NodeId = context.Node.Id,
                NodeName = context.Node.Name,
                Kind = Kind,
                Status = status,
            };
        }
        finally
        {
            Interlocked.Decrement(ref _current);
        }
    }
}

/// <summary>Records requested delays and returns immediately — no real waiting in tests.</summary>
internal sealed class RecordingDelayScheduler : IDelayScheduler
{
    public List<TimeSpan> Requested { get; } = [];

    public Task DelayAsync(TimeSpan duration, CancellationToken cancellationToken = default)
    {
        Requested.Add(duration);
        return Task.CompletedTask;
    }
}

/// <summary>Builds engines and graph fixtures for workflow tests.</summary>
internal static class WorkflowTestHarness
{
    private static readonly DateTimeOffset Now = new(2026, 7, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static WorkflowEngine CreateEngine(
        IRequestExecutor? executor = null,
        IDelayScheduler? scheduler = null,
        IProfileRepository? profiles = null,
        IVariableScopeSeeder? scopeSeeder = null,
        params INodeHandler[] extraHandlers)
    {
        var authApplicator = new AuthApplicator(new FakeSecretProtector());
        var handlers = new List<INodeHandler>
        {
            new RequestNodeHandler(executor ?? new FakeRequestExecutor(), profiles ?? new InMemoryProfileRepository(), authApplicator),
            new ConditionNodeHandler(),
            new LoopNodeHandler(),
            new ParallelNodeHandler(),
            new DelayNodeHandler(scheduler ?? new RecordingDelayScheduler()),
        };
        handlers.AddRange(extraHandlers);

        return new WorkflowEngine(
            new NodeHandlerRegistry(handlers),
            new VariableResolver(),
            new FixedClock(Now),
            scopeSeeder ?? new FakeVariableScopeSeeder());
    }

    public static string ConfigJson<T>(T config) => JsonSerializer.Serialize(config, JsonOptions);

    public static WorkflowNode Node(string name, WorkflowNodeKind kind, string? config = null) => new()
    {
        WorkflowId = Guid.Empty,
        Kind = kind,
        Name = name,
        Config = config,
    };

    public static WorkflowEdge Edge(WorkflowNode source, WorkflowNode target, string? sourcePort = null) => new()
    {
        SourceNodeId = source.Id,
        TargetNodeId = target.Id,
        SourcePort = sourcePort,
    };

    public static Workflow Graph(IReadOnlyList<WorkflowNode> nodes, IReadOnlyList<WorkflowEdge> edges) => new()
    {
        Name = "Test Workflow",
        Nodes = nodes,
        Edges = edges,
    };
}
