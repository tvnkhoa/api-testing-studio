using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Plugin.Abstractions.Runners;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTestingStudio.Runner.Stress;

/// <summary>Plugin module for the stress-testing runner.</summary>
public sealed class StressRunnerPluginModule : IPluginModule
{
    public string Name => "Runner.Stress";

    public Version Version => new(1, 0, 0);

    public void ConfigureServices(IServiceCollection services)
        => services.AddSingleton<IStressRunner, StressRunner>();
}

/// <summary>
/// Placeholder stress runner. Sequential/loop/concurrent execution and metric collection are
/// implemented in the Stress Runner sprint (Sprint 12).
/// </summary>
public sealed class StressRunner : IStressRunner
{
    public Task<StressMetrics> RunAsync(StressPlan plan, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Stress running is delivered in Sprint 12 (Stress Runner).");
}
