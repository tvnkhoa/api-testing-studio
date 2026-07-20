using ApiTestingStudio.Application.Abstractions;

namespace ApiTestingStudio.Core.Plugins;

/// <summary>
/// Adapts the <see cref="IPluginRegistry"/> to the Application <see cref="IInstalledPluginCatalog"/>
/// port. Reports the ids of loaded (non-quarantined) plugins so package import can flag missing
/// dependencies. See ADR-0012.
/// </summary>
public sealed class PluginRegistryInventory : IInstalledPluginCatalog
{
    private readonly IPluginRegistry _registry;

    public PluginRegistryInventory(IPluginRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    public IReadOnlyCollection<string> InstalledPluginIds =>
        _registry.Plugins
            .Where(p => p.State == PluginLifecycleState.Loaded)
            .Select(p => p.Id ?? p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
