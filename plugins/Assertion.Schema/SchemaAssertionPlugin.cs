using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Plugin.Abstractions.Assertions;
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
/// Placeholder schema assertion. Evaluation is implemented in the Assertions sprint (Sprint 11).
/// </summary>
public sealed class SchemaAssertion : IAssertion
{
    public string Kind => "schema";

    public Task<AssertionEvaluation> EvaluateAsync(AssertionContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(new AssertionEvaluation(AssertionOutcome.Skipped, "Schema assertion not yet implemented (Sprint 11)."));
}
