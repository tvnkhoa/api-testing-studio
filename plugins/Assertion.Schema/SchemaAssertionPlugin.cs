using System.Text.Json;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Plugin.Abstractions.Assertions;
using Json.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTestingStudio.Assertion.Schema;

/// <summary>Plugin module for JSON-schema validation assertions.</summary>
public sealed class SchemaAssertionPluginModule : IPluginModule
{
    public string Name => "Assertion.Schema";

    public Version Version => new(1, 0, 0);

    public void ConfigureServices(IServiceCollection services)
        => services.AddSingleton<IAssertion, SchemaAssertion>();
}

/// <summary>
/// Evaluates a schema assertion: validates the actual JSON (<see cref="AssertionContext.Actual"/>)
/// against the JSON Schema supplied in <see cref="AssertionContext.Expected"/>. On failure the
/// validation errors are included in the message. Malformed schema/instance never throws — it
/// becomes a <see cref="AssertionOutcome.Failed"/> with a reason.
/// </summary>
public sealed class SchemaAssertion : IAssertion
{
    public string Kind => "schema";

    public Task<AssertionEvaluation> EvaluateAsync(AssertionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(Evaluate(context));
    }

    private static AssertionEvaluation Evaluate(AssertionContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Expected))
        {
            return new AssertionEvaluation(AssertionOutcome.Skipped, "No JSON Schema provided.");
        }

        JsonSchema schema;
        try
        {
            schema = JsonSchema.FromText(context.Expected);
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            return Fail($"Invalid JSON Schema: {ex.Message}");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(string.IsNullOrEmpty(context.Actual) ? "null" : context.Actual);
        }
        catch (JsonException ex)
        {
            return Fail($"Actual value is not valid JSON: {ex.Message}");
        }

        using (document)
        {
            var results = schema.Evaluate(document.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
            return results.IsValid
                ? Pass("Instance is valid against the schema.")
                : Fail($"Instance failed schema validation: {CollectErrors(results)}");
        }
    }

    private static string CollectErrors(EvaluationResults results)
    {
        var messages = new List<string>();
        Collect(results, messages);
        return messages.Count > 0 ? string.Join("; ", messages) : "instance did not satisfy the schema.";
    }

    private static void Collect(EvaluationResults node, List<string> messages)
    {
        if (node.Errors is not null)
        {
            foreach (var error in node.Errors)
            {
                messages.Add($"{node.InstanceLocation}: {error.Value}");
            }
        }

        if (node.Details is not null)
        {
            foreach (var child in node.Details)
            {
                Collect(child, messages);
            }
        }
    }

    private static AssertionEvaluation Pass(string message) => new(AssertionOutcome.Passed, message);

    private static AssertionEvaluation Fail(string message) => new(AssertionOutcome.Failed, message);
}
