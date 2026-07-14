using System.Reflection;
using ApiTestingStudio.Core.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiTestingStudio.Core.DependencyInjection;

/// <summary>
/// Composition-root helpers that wire the plugin host into the DI container. The host calls
/// <see cref="AddPluginHost"/> with the plugin assemblies; each discovered module then
/// contributes its own services.
/// </summary>
public static class PluginHostServiceCollectionExtensions
{
    /// <summary>
    /// Discovers plugin modules in <paramref name="pluginAssemblies"/>, lets each register its
    /// services, and registers an <see cref="IPluginRegistry"/> describing what was loaded.
    /// </summary>
    public static IServiceCollection AddPluginHost(
        this IServiceCollection services,
        IEnumerable<Assembly> pluginAssemblies,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(pluginAssemblies);

        var logger = loggerFactory?.CreateLogger("ApiTestingStudio.Core.PluginHost");
        var loader = new PluginLoader(loggerFactory?.CreateLogger<PluginLoader>());
        var modules = loader.Discover(pluginAssemblies);

        var descriptors = new List<PluginDescriptor>(modules.Count);
        foreach (var module in modules)
        {
            module.ConfigureServices(services);
            descriptors.Add(new PluginDescriptor(
                module.Name,
                module.Version,
                module.GetType().Assembly.GetName().Name ?? module.GetType().Assembly.FullName ?? "unknown"));
        }

        logger?.LogInformation("Plugin host registered {Count} plugin module(s).", descriptors.Count);

        services.AddSingleton<IPluginRegistry>(new PluginRegistry(descriptors));
        return services;
    }
}
