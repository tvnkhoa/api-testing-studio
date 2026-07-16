using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using FluentAssertions;
using Xunit;
using static ApiTestingStudio.Application.Tests.WorkflowTestHarness;

namespace ApiTestingStudio.Application.Tests;

public sealed class WorkflowEngineProgressTests
{
    [Fact]
    public async Task RunAsync_reports_running_then_final_for_each_node()
    {
        var engine = CreateEngine();
        var a = Node("A", WorkflowNodeKind.Delay, ConfigJson(new DelayNodeConfig { DelayMs = 0 }));
        var b = Node("B", WorkflowNodeKind.Delay, ConfigJson(new DelayNodeConfig { DelayMs = 0 }));
        var graph = Graph([a, b], [Edge(a, b)]);

        var reports = new List<NodeRunResult>();
        await engine.RunAsync(graph, progress: new SyncProgress<NodeRunResult>(reports.Add));

        reports.Where(r => r.NodeId == a.Id).Select(r => r.Status)
            .Should().ContainInOrder(RunStatus.Running, RunStatus.Passed);
        reports.Where(r => r.NodeId == b.Id).Select(r => r.Status)
            .Should().ContainInOrder(RunStatus.Running, RunStatus.Passed);
    }

    [Fact]
    public async Task RunAsync_without_progress_still_completes()
    {
        var engine = CreateEngine();
        var node = Node("A", WorkflowNodeKind.Delay, ConfigJson(new DelayNodeConfig { DelayMs = 0 }));

        var run = await engine.RunAsync(Graph([node], []));

        run.Status.Should().Be(RunStatus.Passed);
    }

    /// <summary>Invokes the report callback synchronously so ordering is deterministic in tests.</summary>
    private sealed class SyncProgress<T> : IProgress<T>
    {
        private readonly Action<T> _report;

        public SyncProgress(Action<T> report) => _report = report;

        public void Report(T value) => _report(value);
    }
}
