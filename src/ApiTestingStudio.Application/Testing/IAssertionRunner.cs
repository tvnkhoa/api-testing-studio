using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Testing;

/// <summary>
/// Evaluates a set of <see cref="AssertionDefinition"/>s against an HTTP execution result. This is
/// the single place that maps a response field (status / reason / header / body / timing) onto the
/// flat <c>Actual</c> string handed to the assertion plugins, so a test case and a workflow
/// Assertion node evaluate against an identical, consistent context.
/// </summary>
public interface IAssertionRunner
{
    /// <summary>
    /// Evaluates each assertion against <paramref name="execution"/> and returns one
    /// <see cref="AssertionResult"/> per definition (disabled ones and unknown kinds come back as
    /// <c>Skipped</c> with a reason). Never throws for assertion-content problems.
    /// </summary>
    Task<IReadOnlyList<AssertionResult>> EvaluateAsync(
        HttpExecutionResult execution,
        IReadOnlyList<AssertionDefinition> assertions,
        CancellationToken cancellationToken = default);
}
