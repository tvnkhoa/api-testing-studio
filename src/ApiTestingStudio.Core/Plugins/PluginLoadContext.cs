using System.Reflection;
using System.Runtime.Loader;

namespace ApiTestingStudio.Core.Plugins;

/// <summary>
/// A collectible <see cref="AssemblyLoadContext"/> that isolates a directory-loaded plugin and its
/// private dependencies. Shared contract assemblies (and anything the default context already
/// provides) resolve from <see cref="AssemblyLoadContext.Default"/>, guaranteeing a single
/// <see cref="Type"/> identity for plugin contracts across the host and the plugin.
/// </summary>
/// <remarks>
/// This is the isolation boundary introduced by ADR-0007. The set of shared assemblies must stay
/// aligned with the contract surface (<c>Plugin.Abstractions</c> and the primitive/domain types it
/// exposes) — loading any of them privately would break type identity.
/// </remarks>
public sealed class PluginLoadContext : AssemblyLoadContext
{
    /// <summary>
    /// Assemblies that must always resolve from the default context so that contract types are
    /// identical on both sides of the isolation boundary.
    /// </summary>
    private static readonly HashSet<string> SharedAssemblies = new(StringComparer.OrdinalIgnoreCase)
    {
        "ApiTestingStudio.Plugin.Abstractions",
        "ApiTestingStudio.Domain",
        "ApiTestingStudio.Shared",
        "Microsoft.Extensions.DependencyInjection.Abstractions",
    };

    private readonly AssemblyDependencyResolver _resolver;

    /// <summary>Creates a collectible load context for the plugin whose main assembly is at the given path.</summary>
    public PluginLoadContext(string mainAssemblyPath, string name)
        : base(name, isCollectible: true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mainAssemblyPath);
        _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Returning null defers to the default context. Shared contracts and framework assemblies
        // must load there; only the plugin's own private dependencies load in isolation.
        if (assemblyName.Name is not null && SharedAssemblies.Contains(assemblyName.Name))
        {
            return null;
        }

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is null ? null : LoadFromAssemblyPath(path);
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path is null ? IntPtr.Zero : LoadUnmanagedDllFromPath(path);
    }
}
