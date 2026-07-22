using ApiTestingStudio.Scenarios.Model;

namespace ApiTestingStudio.Scenarios.Execution;

/// <summary>What a provider observed for a single expectation.</summary>
public sealed record ObservedState(bool Success, string Detail);

/// <summary>Outcome of one step.</summary>
public sealed record StepResult(ScenarioStep Step, bool Success, string Detail);

/// <summary>Outcome of one expectation.</summary>
public sealed record ExpectationResult(Expectation Expectation, bool Success, string Detail);

/// <summary>The full result of running one scenario: step + expectation outcomes, timing, screenshots.</summary>
public sealed record ScenarioResult(
    string Name,
    string Goal,
    bool Passed,
    IReadOnlyList<StepResult> Steps,
    IReadOnlyList<ExpectationResult> Expectations,
    IReadOnlyList<string> Screenshots,
    long DurationMs,
    string? Error);
