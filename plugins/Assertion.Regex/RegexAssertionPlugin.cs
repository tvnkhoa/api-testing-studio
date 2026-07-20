using System.Text.RegularExpressions;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Plugin.Abstractions.Assertions;
using Microsoft.Extensions.DependencyInjection;

// The class name Regex collides with this plugin's own namespace segment; alias it explicitly.
using RegexEngine = System.Text.RegularExpressions.Regex;

namespace ApiTestingStudio.Assertion.Regex;

/// <summary>Plugin module for regular-expression assertions.</summary>
public sealed class RegexAssertionPluginModule : IPluginModule
{
    public string Name => "Assertion.Regex";

    public Version Version => new(1, 0, 0);

    public void ConfigureServices(IServiceCollection services)
        => services.AddSingleton<IAssertion, RegexAssertion>();
}

/// <summary>
/// Evaluates a regex assertion: matches the actual value against a pattern
/// (<c>Options["pattern"]</c>, falling back to <see cref="AssertionContext.Expected"/>). Operator
/// (<c>Options["operator"]</c>) is <c>matches</c> (default) or <c>notMatches</c>;
/// <c>Options["ignoreCase"]</c> enables case-insensitive matching. Capture groups are surfaced in
/// the message. Invalid patterns and match timeouts never throw — they become a
/// <see cref="AssertionOutcome.Failed"/> with a reason.
/// </summary>
public sealed class RegexAssertion : IAssertion
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(2);

    public string Kind => "regex";

    public Task<AssertionEvaluation> EvaluateAsync(AssertionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(Evaluate(context));
    }

    private static AssertionEvaluation Evaluate(AssertionContext context)
    {
        var pattern = GetOption(context, "pattern", context.Expected ?? string.Empty);
        if (string.IsNullOrEmpty(pattern))
        {
            return new AssertionEvaluation(AssertionOutcome.Skipped, "No regex pattern provided.");
        }

        var op = GetOption(context, "operator", "matches").Trim();

        var options = RegexOptions.None;
        if (string.Equals(GetOption(context, "ignoreCase", "false"), "true", StringComparison.OrdinalIgnoreCase))
        {
            options |= RegexOptions.IgnoreCase;
        }

        RegexEngine regex;
        try
        {
            regex = new RegexEngine(pattern, options, MatchTimeout);
        }
        catch (ArgumentException ex)
        {
            return Fail($"Invalid regex pattern '{pattern}': {ex.Message}");
        }

        var actual = context.Actual ?? string.Empty;
        Match match;
        try
        {
            match = regex.Match(actual);
        }
        catch (RegexMatchTimeoutException)
        {
            return Fail($"Regex '{pattern}' timed out matching the actual value.");
        }

        if (string.Equals(op, "notMatches", StringComparison.OrdinalIgnoreCase))
        {
            return match.Success
                ? Fail($"Expected no match for '{pattern}' but found '{match.Value}'.")
                : Pass($"No match for '{pattern}', as expected.");
        }

        if (!match.Success)
        {
            return Fail($"Regex '{pattern}' did not match the actual value.");
        }

        return Pass($"Regex '{pattern}' matched '{match.Value}'{DescribeCaptures(match)}.");
    }

    private static string DescribeCaptures(Match match)
    {
        if (match.Groups.Count <= 1)
        {
            return string.Empty;
        }

        var captures = new List<string>();
        for (var i = 1; i < match.Groups.Count; i++)
        {
            var group = match.Groups[i];
            if (group.Success)
            {
                captures.Add($"{group.Name}='{group.Value}'");
            }
        }

        return captures.Count > 0 ? $" (captures: {string.Join(", ", captures)})" : string.Empty;
    }

    private static AssertionEvaluation Pass(string message) => new(AssertionOutcome.Passed, message);

    private static AssertionEvaluation Fail(string message) => new(AssertionOutcome.Failed, message);

    private static string GetOption(AssertionContext context, string key, string fallback)
        => context.Options is not null && context.Options.TryGetValue(key, out var value) && value is not null
            ? value
            : fallback;
}
