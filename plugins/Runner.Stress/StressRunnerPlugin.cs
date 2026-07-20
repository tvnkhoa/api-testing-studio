using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Plugin.Abstractions.Runners;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTestingStudio.Runner.Stress;

/// <summary>
/// Plugin module for the stress-testing runner. Registering <see cref="IStressRunner"/> is what
/// declares the <see cref="PluginCapability.StressRunner"/> capability — the host infers it from the
/// service collection (see <c>PluginCapabilityMap</c>); there is no marker interface.
/// </summary>
public sealed class StressRunnerPluginModule : IPluginModule
{
    public string Name => "Runner.Stress";

    public Version Version => new(1, 0, 0);

    public void ConfigureServices(IServiceCollection services)
        => services.AddSingleton<IStressRunner, StressRunner>();
}
