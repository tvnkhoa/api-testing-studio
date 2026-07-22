using ApiTestingStudio.Application.Workflows;
using FluentAssertions;

namespace ApiTestingStudio.Application.Tests;

public sealed class VariableResolverTests
{
    private readonly VariableResolver _sut = new();

    [Fact]
    public void Resolve_returns_template_unchanged_when_no_tokens()
    {
        var context = new WorkflowContext();

        _sut.Resolve("plain text", context).Should().Be("plain text");
    }

    [Fact]
    public void Resolve_returns_empty_for_null_or_empty_template()
    {
        var context = new WorkflowContext();

        _sut.Resolve(null, context).Should().BeEmpty();
        _sut.Resolve(string.Empty, context).Should().BeEmpty();
    }

    [Fact]
    public void Resolve_substitutes_a_variable()
    {
        var context = new WorkflowContext();
        context.SetVariable("env", "prod");

        _sut.Resolve("region-{{vars.env}}", context).Should().Be("region-prod");
    }

    [Fact]
    public void Resolve_substitutes_a_node_output()
    {
        var context = new WorkflowContext();
        context.SetNodeOutputs("Login", new Dictionary<string, string> { ["status"] = "200" });

        _sut.Resolve("{{Login.status}}", context).Should().Be("200");
    }

    [Fact]
    public void Resolve_walks_into_a_json_object_output()
    {
        var context = new WorkflowContext();
        context.SetNodeOutputs("Login", new Dictionary<string, string>
        {
            ["body"] = "{\"data\":{\"token\":\"abc123\"}}",
        });

        _sut.Resolve("Bearer {{Login.body.data.token}}", context).Should().Be("Bearer abc123");
    }

    [Fact]
    public void Resolve_indexes_into_a_json_array()
    {
        var context = new WorkflowContext();
        context.SetNodeOutputs("List", new Dictionary<string, string>
        {
            ["body"] = "{\"items\":[\"first\",\"second\"]}",
        });

        _sut.Resolve("{{List.body.items.1}}", context).Should().Be("second");
    }

    [Fact]
    public void Resolve_replaces_unknown_tokens_with_empty()
    {
        var context = new WorkflowContext();

        _sut.Resolve("x={{Missing.key}}", context).Should().Be("x=");
    }

    [Fact]
    public void Resolve_handles_multiple_tokens()
    {
        var context = new WorkflowContext();
        context.SetVariable("a", "1");
        context.SetVariable("b", "2");

        _sut.Resolve("{{vars.a}}-{{vars.b}}", context).Should().Be("1-2");
    }

    [Fact]
    public void TryResolveToken_returns_false_for_missing_path()
    {
        var context = new WorkflowContext();
        context.SetNodeOutputs("Login", new Dictionary<string, string> { ["body"] = "{\"a\":1}" });

        _sut.TryResolveToken("Login.body.missing", context, out _).Should().BeFalse();
    }

    [Fact]
    public void Resolve_with_tracking_collects_unresolved_tokens()
    {
        var context = new WorkflowContext();
        context.SetVariable("env", "prod");
        var unresolved = new List<string>();

        var result = _sut.Resolve("{{vars.env}}/{{vars.missing}}/{{Absent.key}}", context, unresolved);

        result.Should().Be("prod//");
        unresolved.Should().BeEquivalentTo(["vars.missing", "Absent.key"]);
    }

    [Fact]
    public void Resolve_with_tracking_reports_nothing_when_all_resolve()
    {
        var context = new WorkflowContext();
        context.SetVariable("env", "prod");
        var unresolved = new List<string>();

        _sut.Resolve("region-{{vars.env}}", context, unresolved).Should().Be("region-prod");
        unresolved.Should().BeEmpty();
    }
}
