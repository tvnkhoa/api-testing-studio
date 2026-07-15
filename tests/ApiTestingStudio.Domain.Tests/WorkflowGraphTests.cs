using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using FluentAssertions;

namespace ApiTestingStudio.Domain.Tests;

public sealed class WorkflowGraphTests
{
    [Fact]
    public void Workflow_has_an_id_and_empty_graph_by_default()
    {
        var workflow = new Workflow { Name = "Flow" };

        workflow.Id.Should().NotBe(Guid.Empty);
        workflow.Nodes.Should().BeEmpty();
        workflow.Edges.Should().BeEmpty();
    }

    [Fact]
    public void Workflow_composes_its_nodes_and_edges()
    {
        var a = new WorkflowNode { Name = "A", Kind = WorkflowNodeKind.Api };
        var b = new WorkflowNode { Name = "B", Kind = WorkflowNodeKind.Condition };
        var edge = new WorkflowEdge { SourceNodeId = a.Id, TargetNodeId = b.Id };

        var workflow = new Workflow { Name = "Flow", Nodes = [a, b], Edges = [edge] };

        workflow.Nodes.Should().HaveCount(2);
        workflow.Edges.Should().ContainSingle();
        workflow.Edges[0].SourceNodeId.Should().Be(a.Id);
        workflow.Edges[0].TargetNodeId.Should().Be(b.Id);
    }

    [Fact]
    public void WorkflowNode_defaults_are_sensible()
    {
        var node = new WorkflowNode { Name = "N", Kind = WorkflowNodeKind.Delay };

        node.Id.Should().NotBe(Guid.Empty);
        node.Config.Should().BeNull();
        node.PositionX.Should().Be(0);
        node.PositionY.Should().Be(0);
    }

    [Fact]
    public void NodeRunResult_defaults_to_pending_with_empty_children()
    {
        var result = new NodeRunResult { NodeName = "N" };

        result.Status.Should().Be(RunStatus.Pending);
        result.Outputs.Should().BeEmpty();
        result.Children.Should().BeEmpty();
        result.Iteration.Should().BeNull();
    }

    [Fact]
    public void WorkflowRunResult_defaults_to_pending_with_no_nodes()
    {
        var run = new WorkflowRunResult();

        run.Status.Should().Be(RunStatus.Pending);
        run.Nodes.Should().BeEmpty();
        run.Error.Should().BeNull();
    }
}
