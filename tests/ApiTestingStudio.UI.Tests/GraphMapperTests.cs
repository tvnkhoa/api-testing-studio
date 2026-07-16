using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.UI.ViewModels.Workflow;
using FluentAssertions;
using Xunit;

namespace ApiTestingStudio.UI.Tests;

public sealed class GraphMapperTests
{
    private readonly GraphMapper _mapper = new(new NodeViewModelFactory());

    [Fact]
    public void ToViewModel_then_ToDomain_round_trips_nodes_edges_and_config()
    {
        var api = new WorkflowNode
        {
            Kind = WorkflowNodeKind.Api,
            Name = "Login",
            PositionX = 10,
            PositionY = 20,
            Width = 120,
            Height = 60,
            Color = "#123456",
            Config = NodeConfigSerializer.SerializeObject(new RequestNodeConfig { Url = "https://x", Method = HttpVerb.Post }),
        };
        var condition = new WorkflowNode
        {
            Kind = WorkflowNodeKind.Condition,
            Name = "Check",
            PositionX = 200,
            PositionY = 30,
        };
        var edge = new WorkflowEdge
        {
            SourceNodeId = api.Id,
            TargetNodeId = condition.Id,
            SourcePort = WorkflowPorts.Next,
            TargetPort = WorkflowPorts.In,
        };
        var workspaceId = Guid.NewGuid();
        var workflow = new Workflow
        {
            WorkspaceId = workspaceId,
            Name = "W",
            Nodes = [api, condition],
            Edges = [edge],
        };

        var (nodes, connections) = _mapper.ToViewModel(workflow);
        var roundTripped = GraphMapper.ToDomain(workflow.Id, workspaceId, "W", null, nodes, connections);

        nodes.Should().HaveCount(2);
        connections.Should().ContainSingle();

        var apiBack = roundTripped.Nodes.Single(n => n.Id == api.Id);
        apiBack.PositionX.Should().Be(10);
        apiBack.Width.Should().Be(120);
        apiBack.Color.Should().Be("#123456");
        NodeConfigSerializer.Deserialize<RequestNodeConfig>(apiBack.Config)!.Url.Should().Be("https://x");

        roundTripped.Edges.Should().ContainSingle(e =>
            e.SourceNodeId == api.Id
            && e.TargetNodeId == condition.Id
            && e.SourcePort == WorkflowPorts.Next
            && e.TargetPort == WorkflowPorts.In);
    }
}
