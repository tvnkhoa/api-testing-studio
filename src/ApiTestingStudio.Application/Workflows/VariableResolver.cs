using System.Text.Json;
using System.Text.RegularExpressions;

namespace ApiTestingStudio.Application.Workflows;

/// <inheritdoc />
public sealed partial class VariableResolver : IVariableResolver
{
    [GeneratedRegex(@"\{\{\s*(?<expr>[^{}]+?)\s*\}\}")]
    private static partial Regex TokenRegex();

    public string Resolve(string? template, IWorkflowContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrEmpty(template))
        {
            return template ?? string.Empty;
        }

        return TokenRegex().Replace(
            template,
            match => TryResolveToken(match.Groups["expr"].Value, context, out var value)
                ? value ?? string.Empty
                : string.Empty);
    }

    public string Resolve(string? template, IWorkflowContext context, ICollection<string> unresolvedTokens)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(unresolvedTokens);
        if (string.IsNullOrEmpty(template))
        {
            return template ?? string.Empty;
        }

        return TokenRegex().Replace(
            template,
            match =>
            {
                var expr = match.Groups["expr"].Value;
                if (TryResolveToken(expr, context, out var value))
                {
                    return value ?? string.Empty;
                }

                unresolvedTokens.Add(expr.Trim());
                return string.Empty;
            });
    }

    public bool TryResolveToken(string token, IWorkflowContext context, out string? value)
    {
        ArgumentNullException.ThrowIfNull(context);
        value = null;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var segments = token.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return false;
        }

        string? baseValue;
        int pathStart;

        if (segments.Length >= 2 &&
            (segments[0].Equals("vars", StringComparison.OrdinalIgnoreCase) ||
             segments[0].Equals("var", StringComparison.OrdinalIgnoreCase)))
        {
            if (!context.TryGetVariable(segments[1], out baseValue))
            {
                return false;
            }

            pathStart = 2;
        }
        else if (segments.Length >= 2)
        {
            if (!context.TryGetNodeOutputs(segments[0], out var outputs) ||
                !outputs.TryGetValue(segments[1], out baseValue))
            {
                return false;
            }

            pathStart = 2;
        }
        else
        {
            if (!context.TryGetVariable(segments[0], out baseValue))
            {
                return false;
            }

            pathStart = 1;
        }

        if (pathStart >= segments.Length)
        {
            value = baseValue;
            return true;
        }

        return TryTraverseJson(baseValue, segments, pathStart, out value);
    }

    private static bool TryTraverseJson(string? json, string[] segments, int start, out string? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var element = document.RootElement;

            for (var i = start; i < segments.Length; i++)
            {
                var segment = segments[i];
                if (element.ValueKind == JsonValueKind.Object)
                {
                    if (!element.TryGetProperty(segment, out element))
                    {
                        return false;
                    }
                }
                else if (element.ValueKind == JsonValueKind.Array && int.TryParse(segment, out var index))
                {
                    if (index < 0 || index >= element.GetArrayLength())
                    {
                        return false;
                    }

                    element = element[index];
                }
                else
                {
                    return false;
                }
            }

            value = element.ValueKind == JsonValueKind.String ? element.GetString() : element.GetRawText();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
