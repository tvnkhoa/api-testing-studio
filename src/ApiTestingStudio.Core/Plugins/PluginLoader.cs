using System.Reflection;
using ApiTestingStudio.Plugin.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiTestingStudio.Core.Plugins;

/// <summary>
/// Discovers <see cref="IPluginModule"/> implementations by reflecting over a supplied set of
/// assemblies. This is the ONLY place that turns assemblies into plugin instances; the core
/// never references a concrete plugin.
/// </summary>
/// <remarks>
/// Phase 1 receives assemblies that the host has already referenced. The design intentionally
/// takes an assembly list so a future directory-scanning / <c>AssemblyLoadContext</c> mode can
/// supply assemblies here without any change to the discovery logic or business code.
/// </remarks>
public sealed class PluginLoader
{
    private readonly ILogger<PluginLoader> _logger;

    public PluginLoader(ILogger<PluginLoader>? logger = null)
        => _logger = logger ?? NullLogger<PluginLoader>.Instance;

    /// <summary>
    /// Instantiates every non-abstract <see cref="IPluginModule"/> found in the given assemblies.
    /// </summary>
    public IReadOnlyList<IPluginModule> Discover(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var modules = new List<IPluginModule>();

        foreach (var assembly in assemblies.Distinct())
        {
            foreach (var type in GetLoadableTypes(assembly))
            {
                if (type is { IsAbstract: false, IsInterface: false } &&
                    typeof(IPluginModule).IsAssignableFrom(type))
                {
                    if (Activator.CreateInstance(type) is IPluginModule module)
                    {
                        _logger.LogInformation(
                            "Discovered plugin module {PluginName} v{PluginVersion} from {Assembly}.",
                            module.Name,
                            module.Version,
                            assembly.GetName().Name);
                        modules.Add(module);
                    }
                }
            }
        }

        return modules;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Return the types that DID load; a partially-loadable plugin assembly should not
            // take down discovery of the others.
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
