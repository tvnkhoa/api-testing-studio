using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Core.Plugins;

/// <summary>
/// Immutable metadata describing a plugin the host attempted to load — whether it loaded
/// successfully or was quarantined. Holds only value data (no references into a plugin's load
/// context) so the registry never roots a collectible <c>AssemblyLoadContext</c>.
/// </summary>
/// <param name="Name">Plugin module name (e.g. "Import.Curl").</param>
/// <param name="Version">The plugin's own version.</param>
/// <param name="AssemblyName">Simple name of the assembly the module came from.</param>
/// <param name="Source">Whether the plugin was compile-time or directory-loaded.</param>
/// <param name="State">Outcome of the load attempt (<c>Loaded</c> or <c>Quarantined</c>).</param>
/// <param name="Capabilities">Contract categories the plugin contributed (inferred from its services).</param>
/// <param name="Id">Manifest id for directory plugins; the module name for compile-time plugins.</param>
/// <param name="Error">The typed reason a plugin was quarantined, or <c>null</c> when loaded.</param>
public sealed record PluginDescriptor(
    string Name,
    Version Version,
    string AssemblyName,
    PluginSource Source,
    PluginLifecycleState State,
    IReadOnlyList<PluginCapability> Capabilities,
    string? Id = null,
    Error? Error = null);
