using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Plugin.Abstractions.Assertions;
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
/// Placeholder JSON assertion. Evaluation is implemented in the Assertions sprint (Sprint 11).
/// </summary>
public sealed class JsonAssertion : IAssertion
{
    public string Kind => "json";

    public Task<AssertionEvaluation> EvaluateAsync(AssertionContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(new AssertionEvaluation(AssertionOutcome.Skipped, "JSON assertion not yet implemented (Sprint 11)."));
}
