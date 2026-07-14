using System.Reflection;
using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Core.Plugins;

/// <summary>A plugin module paired with the load context it was loaded into (if any).</summary>
/// <param name="Module">The instantiated plugin entry point.</param>
/// <param name="Context">
/// The isolated load context for a directory plugin, or <c>null</c> for a compile-time plugin
/// loaded into the default context.
/// </param>
public sealed record LoadedPlugin(IPluginModule Module, PluginLoadContext? Context);

/// <summary>
/// Turns assemblies and directory candidates into <see cref="IPluginModule"/> instances. This is
/// the only place that materialises plugins; the core never references a concrete plugin.
/// </summary>
public interface IPluginLoader
{
    /// <summary>Instantiates every non-abstract <see cref="IPluginModule"/> found in the given assemblies.</summary>
    IReadOnlyList<IPluginModule> Discover(IEnumerable<Assembly> assemblies);

    /// <summary>
    /// Loads a directory plugin into an isolated <see cref="PluginLoadContext"/> and instantiates
    /// its module. Never throws for a bad plugin — failures come back as a failed result and any
    /// partially created load context is unloaded.
    /// </summary>
    Result<LoadedPlugin> Load(DirectoryPluginCandidate candidate);
}
