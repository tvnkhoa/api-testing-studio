using System.Collections.Generic;
using ApiTestingStudio.Assertion.Json;
using ApiTestingStudio.Assertion.Regex;
using ApiTestingStudio.Assertion.Schema;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Plugin.Abstractions.Assertions;
using FluentAssertions;

namespace ApiTestingStudio.PluginHost.Tests;

/// <summary>Evaluation behaviour of the Sprint 11 assertion plugins (JSON / regex / schema).</summary>
public sealed class AssertionEvaluationTests
{
    private static AssertionContext Context(string actual, string expected, params (string Key, string Value)[] options)
    {
        var dict = new Dictionary<string, string>();
        foreach (var (key, value) in options)
        {
            dict[key] = value;
        }

        return new AssertionContext(actual, expected, dict);
    }

    [Fact]
    public async Task Json_equals_on_jsonpath_selection_passes()
    {
        var result = await new JsonAssertion().EvaluateAsync(
            Context("{\"data\":{\"id\":42,\"name\":\"ada\"}}", "ada", ("path", "$.data.name"), ("operator", "equals")));

        result.Outcome.Should().Be(AssertionOutcome.Passed);
    }

    [Fact]
    public async Task Json_equals_reports_actual_on_failure()
    {
        var result = await new JsonAssertion().EvaluateAsync(
            Context("{\"data\":{\"name\":\"bob\"}}", "ada", ("path", "$.data.name")));

        result.Outcome.Should().Be(AssertionOutcome.Failed);
        result.Message.Should().Contain("ada").And.Contain("bob");
    }

    [Fact]
    public async Task Json_numeric_greater_than_passes()
    {
        var result = await new JsonAssertion().EvaluateAsync(
            Context("{\"count\":10}", "5", ("path", "$.count"), ("operator", "gt")));

        result.Outcome.Should().Be(AssertionOutcome.Passed);
    }

    [Fact]
    public async Task Json_exists_operator_detects_missing_node()
    {
        var result = await new JsonAssertion().EvaluateAsync(
            Context("{\"a\":1}", string.Empty, ("path", "$.missing"), ("operator", "exists")));

        result.Outcome.Should().Be(AssertionOutcome.Failed);
    }

    [Fact]
    public async Task Json_malformed_input_fails_without_throwing()
    {
        var result = await new JsonAssertion().EvaluateAsync(
            Context("not json", "x", ("path", "$.a")));

        result.Outcome.Should().Be(AssertionOutcome.Failed);
        result.Message.Should().Contain("valid JSON");
    }

    [Fact]
    public async Task Regex_match_with_capture_passes()
    {
        var result = await new RegexAssertion().EvaluateAsync(
            Context("order-12345 created", "order-(?<id>\\d+)"));

        result.Outcome.Should().Be(AssertionOutcome.Passed);
        result.Message.Should().Contain("12345");
    }

    [Fact]
    public async Task Regex_no_match_fails()
    {
        var result = await new RegexAssertion().EvaluateAsync(
            Context("hello", "^\\d+$"));

        result.Outcome.Should().Be(AssertionOutcome.Failed);
    }

    [Fact]
    public async Task Regex_invalid_pattern_fails_without_throwing()
    {
        var result = await new RegexAssertion().EvaluateAsync(
            Context("anything", "([unclosed"));

        result.Outcome.Should().Be(AssertionOutcome.Failed);
        result.Message.Should().Contain("Invalid regex");
    }

    [Fact]
    public async Task Schema_valid_instance_passes()
    {
        const string schema = """
        { "type": "object", "required": ["id"], "properties": { "id": { "type": "integer" } } }
        """;

        var result = await new SchemaAssertion().EvaluateAsync(Context("{\"id\":7}", schema));

        result.Outcome.Should().Be(AssertionOutcome.Passed);
    }

    [Fact]
    public async Task Schema_invalid_instance_fails_with_reason()
    {
        const string schema = """
        { "type": "object", "required": ["id"], "properties": { "id": { "type": "integer" } } }
        """;

        var result = await new SchemaAssertion().EvaluateAsync(Context("{\"id\":\"not-a-number\"}", schema));

        result.Outcome.Should().Be(AssertionOutcome.Failed);
        result.Message.Should().Contain("schema validation");
    }
}
