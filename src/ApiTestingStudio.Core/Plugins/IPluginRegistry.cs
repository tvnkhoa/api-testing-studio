namespace ApiTestingStudio.Core.Plugins;

/// <summary>
/// Read-only view of the plugins that were discovered and registered at startup.
/// Resolved from DI so any part of the app can enumerate active plugins.
/// </summary>
public interface IPluginRegistry
{
    IReadOnlyList<PluginDescriptor> Plugins { get; }
}

/// <summary>Default in-memory registry populated by the plugin host during composition.</summary>
public sealed class PluginRegistry : IPluginRegistry
{
    public PluginRegistry(IReadOnlyList<PluginDescriptor> plugins) => Plugins = plugins;

    public IReadOnlyList<PluginDescriptor> Plugins { get; }
}
