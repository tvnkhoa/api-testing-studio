using ApiTestingStudio.Plugin.Abstractions;

namespace ApiTestingStudio.Core.Plugins;

/// <summary>
/// Read-only view of the plugins the host attempted to load (successful and quarantined).
/// Resolved from DI so any part of the app can enumerate plugins or query them by capability.
/// </summary>
public interface IPluginRegistry
{
    /// <summary>Every plugin the host attempted to load, in discovery order.</summary>
    IReadOnlyList<PluginDescriptor> Plugins { get; }

    /// <summary>Plugins that loaded successfully and contribute the given capability.</summary>
    IReadOnlyList<PluginDescriptor> GetByCapability(PluginCapability capability);
}

/// <summary>Default in-memory registry populated by the plugin host during composition.</summary>
public sealed class PluginRegistry : IPluginRegistry
{
    public PluginRegistry(IReadOnlyList<PluginDescriptor> plugins) => Plugins = plugins;

    public IReadOnlyList<PluginDescriptor> Plugins { get; }

    public IReadOnlyList<PluginDescriptor> GetByCapability(PluginCapability capability) =>
        Plugins.Where(p => p.State == PluginLifecycleState.Loaded && p.Capabilities.Contains(capability)).ToList();
}
