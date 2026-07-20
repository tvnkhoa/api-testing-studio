using ApiTestingStudio.Application.Testing;
using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Application.Workflows.Handlers;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using FluentAssertions;

namespace ApiTestingStudio.Application.Tests;

public sealed class AssertionNodeHandlerTests
{
    private static NodeHandlerContext BuildContext(WorkflowNode node, WorkflowContext context)
    {
        var workflow = new Workflow { Name = "WF", Nodes = [node] };
        return new NodeHandlerContext
        {
            Node = node,
            Workflow = workflow,
            Context = context,
            Resolver = new VariableResolver(),
            Options = new WorkflowRunOptions(),
            RunBranch = (_, _, _) => Task.FromResult<IReadOnlyList<NodeRunResult>>([]),
        };
    }

    [Fact]
    public void Handler_registers_for_the_assertion_kind()
    {
        var handler = new AssertionNodeHandler(new AssertionRunner([new FakeAssertion("json")]));
        handler.Kind.Should().Be(WorkflowNodeKind.Assertion);
    }

    [Fact]
    public async Task Passes_when_upstream_outputs_satisfy_assertions()
    {
        var handler = new AssertionNodeHandler(new AssertionRunner([new FakeAssertion("json", AssertionOutcome.Passed)]));

        var config = new AssertionNodeConfig
        {
            SourceNode = "Login",
            Assertions = [new AssertionSpec { Kind = "json", Source = AssertionSource.StatusCode, Expected = "200" }],
        };
        var node = new WorkflowNode { Kind = WorkflowNodeKind.Assertion, Name = "Assert", Config = NodeConfigSerializer.Serialize(config) };

        var context = new WorkflowContext();
        context.SetNodeOutputs("Login", new Dictionary<string, string> { ["status"] = "200", ["body"] = "{}" });

        var result = await handler.ExecuteAsync(BuildContext(node, context));

        result.Status.Should().Be(RunStatus.Passed);
        result.Outputs["passed"].Should().Be("1");
    }

    [Fact]
    public async Task Fails_when_an_assertion_fails()
    {
        var handler = new AssertionNodeHandler(new AssertionRunner([new FakeAssertion("json", AssertionOutcome.Failed)]));

        var config = new AssertionNodeConfig
        {
            SourceNode = "Login",
            Assertions = [new AssertionSpec { Kind = "json", Source = AssertionSource.Body, Expected = "x" }],
        };
        var node = new WorkflowNode { Kind = WorkflowNodeKind.Assertion, Name = "Assert", Config = NodeConfigSerializer.Serialize(config) };

        var context = new WorkflowContext();
        context.SetNodeOutputs("Login", new Dictionary<string, string> { ["body"] = "{}" });

        var result = await handler.ExecuteAsync(BuildContext(node, context));

        result.Status.Should().Be(RunStatus.Failed);
        result.Error.Should().Contain("assertion");
    }

    [Fact]
    public async Task Fails_when_source_node_has_no_outputs()
    {
        var handler = new AssertionNodeHandler(new AssertionRunner([new FakeAssertion("json")]));

        var config = new AssertionNodeConfig
        {
            SourceNode = "Missing",
            Assertions = [new AssertionSpec { Kind = "json", Source = AssertionSource.Body, Expected = "x" }],
        };
        var node = new WorkflowNode { Kind = WorkflowNodeKind.Assertion, Name = "Assert", Config = NodeConfigSerializer.Serialize(config) };

        var result = await handler.ExecuteAsync(BuildContext(node, new WorkflowContext()));

        result.Status.Should().Be(RunStatus.Failed);
    }
}
