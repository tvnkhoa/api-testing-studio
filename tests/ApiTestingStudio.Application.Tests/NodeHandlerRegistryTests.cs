using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Application.Workflows.Handlers;
using ApiTestingStudio.Domain.Enums;
using FluentAssertions;

namespace ApiTestingStudio.Application.Tests;

public sealed class NodeHandlerRegistryTests
{
    [Fact]
    public void Resolve_returns_the_handler_for_a_registered_kind()
    {
        var registry = new NodeHandlerRegistry([new ConditionNodeHandler()]);

        var result = registry.Resolve(WorkflowNodeKind.Condition);

        result.IsSuccess.Should().BeTrue();
        result.Value.Kind.Should().Be(WorkflowNodeKind.Condition);
    }

    [Fact]
    public void Resolve_fails_for_an_unregistered_kind()
    {
        var registry = new NodeHandlerRegistry([new ConditionNodeHandler()]);

        var result = registry.Resolve(WorkflowNodeKind.Switch);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("workflow.no_handler");
    }

    [Fact]
    public void Constructor_throws_on_duplicate_kind()
    {
        var act = () => new NodeHandlerRegistry([new ConditionNodeHandler(), new ConditionNodeHandler()]);

        act.Should().Throw<InvalidOperationException>();
    }
}
