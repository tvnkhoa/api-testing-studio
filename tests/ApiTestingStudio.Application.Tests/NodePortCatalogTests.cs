using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace ApiTestingStudio.Application.Tests;

public sealed class NodePortCatalogTests
{
    [Fact]
    public void InputPorts_for_any_kind_is_a_single_in_port()
    {
        NodePortCatalog.InputPorts(WorkflowNodeKind.Api).Should().ContainSingle().Which.Should().Be(WorkflowPorts.In);
        NodePortCatalog.InputPorts(WorkflowNodeKind.Condition).Should().ContainSingle().Which.Should().Be(WorkflowPorts.In);
    }

    [Fact]
    public void OutputPorts_for_condition_are_true_and_false()
    {
        NodePortCatalog.OutputPorts(WorkflowNodeKind.Condition)
            .Should().Equal(WorkflowPorts.True, WorkflowPorts.False);
    }

    [Theory]
    [InlineData(WorkflowNodeKind.Loop)]
    [InlineData(WorkflowNodeKind.Parallel)]
    public void OutputPorts_for_container_kinds_are_body_and_next(WorkflowNodeKind kind)
    {
        NodePortCatalog.OutputPorts(kind).Should().Equal(WorkflowPorts.Body, WorkflowPorts.Next);
    }

    [Theory]
    [InlineData(WorkflowNodeKind.Api)]
    [InlineData(WorkflowNodeKind.Delay)]
    [InlineData(WorkflowNodeKind.Assertion)]
    public void OutputPorts_for_leaf_kinds_are_a_single_next_port(WorkflowNodeKind kind)
    {
        NodePortCatalog.OutputPorts(kind).Should().ContainSingle().Which.Should().Be(WorkflowPorts.Next);
    }
}
