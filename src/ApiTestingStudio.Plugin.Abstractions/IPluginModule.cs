using Microsoft.Extensions.DependencyInjection;

namespace ApiTestingStudio.Plugin.Abstractions;

/// <summary>
/// The single entry point every plugin assembly exposes. The plugin host discovers all
/// implementations, instantiates them via their parameterless constructor, and invokes
/// <see cref="ConfigureServices"/> to let the plugin register its own services (importers,
/// assertions, workflow nodes, widgets, …) into the application container.
/// </summary>
/// <remarks>
/// The core never references a concrete plugin. Coupling flows only through this contract,
/// which keeps every capability replaceable. See <c>.claude/PLUGIN_DEVELOPMENT.md</c>.
/// </remarks>
public interface IPluginModule
{
    /// <summary>Stable, human-readable plugin name (e.g. "Import.Curl").</summary>
    string Name { get; }

    /// <summary>Plugin version, used for compatibility and diagnostics.</summary>
    Version Version { get; }

    /// <summary>Registers the plugin's services into the shared DI container.</summary>
    void ConfigureServices(IServiceCollection services);
}
