using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Plugin.Abstractions.Assertions;
using Microsoft.Extensions.DependencyInjection;

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
/// Placeholder regex assertion. Evaluation is implemented in the Assertions sprint (Sprint 11).
/// </summary>
public sealed class RegexAssertion : IAssertion
{
    public string Kind => "regex";

    public Task<AssertionEvaluation> EvaluateAsync(AssertionContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(new AssertionEvaluation(AssertionOutcome.Skipped, "Regex assertion not yet implemented (Sprint 11)."));
}
