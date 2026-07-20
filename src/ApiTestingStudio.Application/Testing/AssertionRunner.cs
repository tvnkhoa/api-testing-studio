using System.Globalization;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Plugin.Abstractions.Assertions;

namespace ApiTestingStudio.Application.Testing;

/// <summary>
/// Default <see cref="IAssertionRunner"/>. Indexes the discovered <see cref="IAssertion"/> plugins by
/// <see cref="IAssertion.Kind"/> (mirroring <c>NodeHandlerRegistry</c>) and dispatches each
/// definition to the matching plugin after resolving its response field into the actual value.
/// </summary>
public sealed class AssertionRunner : IAssertionRunner
{
    private readonly Dictionary<string, IAssertion> _byKind;

    public AssertionRunner(IEnumerable<IAssertion> assertions)
    {
        ArgumentNullException.ThrowIfNull(assertions);
        _byKind = assertions
            .GroupBy(a => a.Kind, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<AssertionResult>> EvaluateAsync(
        HttpExecutionResult execution,
        IReadOnlyList<AssertionDefinition> assertions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(assertions);

        var results = new List<AssertionResult>(assertions.Count);
        foreach (var definition in assertions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await EvaluateOneAsync(execution, definition, cancellationToken).ConfigureAwait(false));
        }

        return results;
    }

    private async Task<AssertionResult> EvaluateOneAsync(
        HttpExecutionResult execution,
        AssertionDefinition definition,
        CancellationToken cancellationToken)
    {
        if (!definition.Enabled)
        {
            return Result(definition, AssertionOutcome.Skipped, "Assertion is disabled.");
        }

        if (!_byKind.TryGetValue(definition.Kind, out var assertion))
        {
            return Result(definition, AssertionOutcome.Skipped, $"No assertion plugin is registered for kind '{definition.Kind}'.");
        }

        var actual = ResolveActual(execution, definition);
        var context = new AssertionContext(actual, definition.Expected, BuildOptions(definition));

        var evaluation = await assertion.EvaluateAsync(context, cancellationToken).ConfigureAwait(false);
        return new AssertionResult
        {
            AssertionId = definition.Id,
            Kind = definition.Kind,
            Outcome = evaluation.Outcome,
            Message = evaluation.Message,
        };
    }

    private static string ResolveActual(HttpExecutionResult execution, AssertionDefinition definition)
    {
        var response = execution.Response;
        return definition.Source switch
        {
            AssertionSource.StatusCode => response.StatusCode.ToString(CultureInfo.InvariantCulture),
            AssertionSource.ReasonPhrase => response.ReasonPhrase,
            AssertionSource.Header => FindHeader(response, definition.Target),
            AssertionSource.Body => response.Body ?? string.Empty,
            AssertionSource.TimingTotalMs => ((long)execution.Timing.Total.TotalMilliseconds).ToString(CultureInfo.InvariantCulture),
            _ => string.Empty,
        };
    }

    private static string FindHeader(HttpResponseModel response, string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        var header = response.Headers.FirstOrDefault(h => string.Equals(h.Name, name, StringComparison.OrdinalIgnoreCase));
        return header?.Value ?? string.Empty;
    }

    private static Dictionary<string, string> BuildOptions(AssertionDefinition definition)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(definition.Expression))
        {
            // Each plugin reads only its own key: JSON uses "path", regex uses "pattern".
            options["path"] = definition.Expression;
            options["pattern"] = definition.Expression;
        }

        if (!string.IsNullOrEmpty(definition.Operator))
        {
            options["operator"] = definition.Operator;
        }

        return options;
    }

    private static AssertionResult Result(AssertionDefinition definition, AssertionOutcome outcome, string message) => new()
    {
        AssertionId = definition.Id,
        Kind = definition.Kind,
        Outcome = outcome,
        Message = message,
    };
}
