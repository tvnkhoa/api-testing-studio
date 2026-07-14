using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Plugin.Abstractions.Assertions;

/// <summary>Inputs available when evaluating an assertion against an actual value.</summary>
public sealed record AssertionContext(string Actual, string Expected, IReadOnlyDictionary<string, string> Options);

/// <summary>The result of evaluating a single assertion.</summary>
public sealed record AssertionEvaluation(AssertionOutcome Outcome, string? Message = null);

/// <summary>
/// Evaluates one kind of assertion (JSON path, regex, schema, …). Implemented by
/// <c>Assertion.*</c> plugins.
/// </summary>
public interface IAssertion
{
    /// <summary>Stable identifier for the assertion kind (e.g. "json", "regex", "schema").</summary>
    string Kind { get; }

    /// <summary>Evaluates the assertion.</summary>
    Task<AssertionEvaluation> EvaluateAsync(AssertionContext context, CancellationToken cancellationToken = default);
}
