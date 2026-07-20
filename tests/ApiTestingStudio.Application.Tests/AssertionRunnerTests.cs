using ApiTestingStudio.Application.Testing;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using FluentAssertions;

namespace ApiTestingStudio.Application.Tests;

public sealed class AssertionRunnerTests
{
    private static HttpExecutionResult Execution() => new()
    {
        Response = new HttpResponseModel
        {
            StatusCode = 201,
            ReasonPhrase = "Created",
            Body = "{\"id\":7}",
            Headers = [new HttpHeader("Content-Type", "application/json")],
        },
        Timing = new RequestTiming { Total = TimeSpan.FromMilliseconds(123) },
    };

    private static AssertionDefinition Def(AssertionSource source, string kind = "json", string? target = null, bool enabled = true) => new()
    {
        Kind = kind,
        Source = source,
        Target = target,
        Expected = "x",
        Enabled = enabled,
    };

    [Fact]
    public async Task Maps_status_code_to_actual()
    {
        var fake = new FakeAssertion("json");
        var runner = new AssertionRunner([fake]);

        await runner.EvaluateAsync(Execution(), [Def(AssertionSource.StatusCode)]);

        fake.LastContext!.Actual.Should().Be("201");
    }

    [Fact]
    public async Task Maps_named_header_to_actual()
    {
        var fake = new FakeAssertion("json");
        var runner = new AssertionRunner([fake]);

        await runner.EvaluateAsync(Execution(), [Def(AssertionSource.Header, target: "content-type")]);

        fake.LastContext!.Actual.Should().Be("application/json");
    }

    [Fact]
    public async Task Maps_body_and_timing_to_actual()
    {
        var fake = new FakeAssertion("json");
        var runner = new AssertionRunner([fake]);

        await runner.EvaluateAsync(Execution(), [Def(AssertionSource.Body)]);
        fake.LastContext!.Actual.Should().Be("{\"id\":7}");

        await runner.EvaluateAsync(Execution(), [Def(AssertionSource.TimingTotalMs)]);
        fake.LastContext!.Actual.Should().Be("123");
    }

    [Fact]
    public async Task Unknown_kind_is_skipped_with_reason()
    {
        var runner = new AssertionRunner([new FakeAssertion("json")]);

        var results = await runner.EvaluateAsync(Execution(), [Def(AssertionSource.Body, kind: "schema")]);

        results.Should().ContainSingle();
        results[0].Outcome.Should().Be(AssertionOutcome.Skipped);
        results[0].Message.Should().Contain("No assertion plugin");
    }

    [Fact]
    public async Task Disabled_assertion_is_skipped()
    {
        var runner = new AssertionRunner([new FakeAssertion("json")]);

        var results = await runner.EvaluateAsync(Execution(), [Def(AssertionSource.Body, enabled: false)]);

        results[0].Outcome.Should().Be(AssertionOutcome.Skipped);
    }

    [Fact]
    public async Task Dispatches_to_matching_kind()
    {
        var json = new FakeAssertion("json", AssertionOutcome.Passed);
        var regex = new FakeAssertion("regex", AssertionOutcome.Failed);
        var runner = new AssertionRunner([json, regex]);

        var results = await runner.EvaluateAsync(Execution(),
        [
            Def(AssertionSource.Body, kind: "json"),
            Def(AssertionSource.Body, kind: "regex"),
        ]);

        results.Should().SatisfyRespectively(
            first => first.Outcome.Should().Be(AssertionOutcome.Passed),
            second => second.Outcome.Should().Be(AssertionOutcome.Failed));
    }
}
