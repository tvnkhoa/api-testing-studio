namespace ApiTestingStudio.Core.Plugins;

/// <summary>Immutable metadata describing a discovered and registered plugin module.</summary>
public sealed record PluginDescriptor(string Name, Version Version, string AssemblyName);
