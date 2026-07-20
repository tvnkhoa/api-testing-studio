using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Plugin.Abstractions.Assertions;
using Json.Path;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTestingStudio.Assertion.Json;

/// <summary>Plugin module for JSON-path / JSON-value assertions.</summary>
public sealed class JsonAssertionPluginModule : IPluginModule
{
    public string Name => "Assertion.Json";

    public Version Version => new(1, 0, 0);

    public void ConfigureServices(IServiceCollection services)
        => services.AddSingleton<IAssertion, JsonAssertion>();
}

/// <summary>
/// Evaluates a JSON assertion: optionally selects a value from the actual JSON via a JSONPath
/// (<c>Options["path"]</c>), then compares it to <see cref="AssertionContext.Expected"/> using an
/// operator (<c>Options["operator"]</c>: equals / notEquals / contains / exists / notExists /
/// gt / gte / lt / lte; defaults to <c>equals</c>). Malformed input never throws — it becomes a
/// <see cref="AssertionOutcome.Failed"/> with a reason.
/// </summary>
public sealed class JsonAssertion : IAssertion
{
    public string Kind => "json";

    public Task<AssertionEvaluation> EvaluateAsync(AssertionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(Evaluate(context));
    }

    private static AssertionEvaluation Evaluate(AssertionContext context)
    {
        var op = GetOption(context, "operator", "equals").Trim();
        var pathExpr = GetOption(context, "path", string.Empty).Trim();

        string? actualValue;
        if (pathExpr.Length > 0)
        {
            JsonNode? root;
            try
            {
                root = JsonNode.Parse(string.IsNullOrEmpty(context.Actual) ? "null" : context.Actual);
            }
            catch (JsonException ex)
            {
                return Fail($"Actual value is not valid JSON: {ex.Message}");
            }

            if (!JsonPath.TryParse(pathExpr, out var path))
            {
                return Fail($"Invalid JSONPath expression '{pathExpr}'.");
            }

            var matches = path.Evaluate(root).Matches;

            if (string.Equals(op, "exists", StringComparison.OrdinalIgnoreCase))
            {
                return matches.Count > 0
                    ? Pass($"JSONPath '{pathExpr}' matched {matches.Count} node(s).")
                    : Fail($"JSONPath '{pathExpr}' matched no nodes.");
            }

            if (string.Equals(op, "notExists", StringComparison.OrdinalIgnoreCase))
            {
                return matches.Count == 0
                    ? Pass($"JSONPath '{pathExpr}' matched no nodes, as expected.")
                    : Fail($"JSONPath '{pathExpr}' matched {matches.Count} node(s) but none were expected.");
            }

            if (matches.Count == 0)
            {
                return Fail($"JSONPath '{pathExpr}' matched no nodes.");
            }

            actualValue = NodeToComparable(matches[0].Value);
        }
        else
        {
            actualValue = context.Actual;
        }

        return Compare(op, actualValue, context.Expected, pathExpr);
    }

    private static AssertionEvaluation Compare(string op, string? actual, string expected, string pathExpr)
    {
        var where = pathExpr.Length > 0 ? $"JSONPath '{pathExpr}'" : "value";
        actual ??= "null";

        return op.ToLowerInvariant() switch
        {
            "equals" => string.Equals(actual, expected, StringComparison.Ordinal)
                ? Pass($"{where} equals '{expected}'.")
                : Fail($"Expected {where} to equal '{expected}' but was '{actual}'."),
            "notequals" => !string.Equals(actual, expected, StringComparison.Ordinal)
                ? Pass($"{where} does not equal '{expected}'.")
                : Fail($"Expected {where} to not equal '{expected}' but it did."),
            "contains" => actual.Contains(expected, StringComparison.Ordinal)
                ? Pass($"{where} contains '{expected}'.")
                : Fail($"Expected {where} '{actual}' to contain '{expected}'."),
            "gt" or "gte" or "lt" or "lte" => CompareNumeric(op, actual, expected, where),
            _ => new AssertionEvaluation(AssertionOutcome.Skipped, $"Unknown JSON operator '{op}'."),
        };
    }

    private static AssertionEvaluation CompareNumeric(string op, string actual, string expected, string where)
    {
        if (!double.TryParse(actual, NumberStyles.Any, CultureInfo.InvariantCulture, out var a) ||
            !double.TryParse(expected, NumberStyles.Any, CultureInfo.InvariantCulture, out var e))
        {
            return Fail($"Cannot compare {where} '{actual}' to '{expected}' numerically.");
        }

        var ok = op.ToLowerInvariant() switch
        {
            "gt" => a > e,
            "gte" => a >= e,
            "lt" => a < e,
            "lte" => a <= e,
            _ => false,
        };

        return ok
            ? Pass($"{where} {op} {expected} (actual {actual}).")
            : Fail($"Expected {where} {op} {expected} but was {actual}.");
    }

    private static string NodeToComparable(JsonNode? node)
    {
        if (node is null)
        {
            return "null";
        }

        // Scalars stringify to their raw value (no quotes); objects/arrays to compact JSON.
        return node is JsonValue ? node.ToString() : node.ToJsonString();
    }

    private static AssertionEvaluation Pass(string message) => new(AssertionOutcome.Passed, message);

    private static AssertionEvaluation Fail(string message) => new(AssertionOutcome.Failed, message);

    private static string GetOption(AssertionContext context, string key, string fallback)
        => context.Options is not null && context.Options.TryGetValue(key, out var value) && value is not null
            ? value
            : fallback;
}
