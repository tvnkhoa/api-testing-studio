using System.Reflection;
using ApiTestingStudio.Core.Plugins;
using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Shared.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiTestingStudio.Core.DependencyInjection;

/// <summary>
/// Composition-root helpers that wire the plugin host into the DI container. The host calls
/// <see cref="AddPluginHost"/> with compile-time plugin assemblies and, optionally, a
/// <c>plugins/</c> directory to scan for drop-in plugins. Both sources feed one registration
/// pipeline: each discovered module contributes its services, capabilities are inferred, and
/// incompatible or throwing plugins are quarantined instead of crashing the host.
/// </summary>
public static class PluginHostServiceCollectionExtensions
{
    /// <summary>Error code recorded when a module's <c>ConfigureServices</c> throws.</summary>
    public const string ConfigureFailedCode = "plugin.configure_failed";

    /// <summary>
    /// Discovers plugins from <paramref name="pluginAssemblies"/> (compile-time) and, when supplied,
    /// from <paramref name="pluginsDirectory"/> (dynamic), registers each plugin's services, records
    /// an <see cref="IPluginRegistry"/>, and wires a <see cref="PluginLifecycleManager"/> hosted
    /// service.
    /// </summary>
    /// <param name="services">The DI container being composed.</param>
    /// <param name="pluginAssemblies">Compile-time plugin assemblies (Phase 1 source).</param>
    /// <param name="pluginsDirectory">Optional root folder scanned for directory plugins.</param>
    /// <param name="loggerFactory">Optional bootstrap logger factory for composition-time logging.</param>
    /// <param name="hostApiVersion">Optional host API version override; defaults to <see cref="PluginApiVersion.Current"/>.</param>
    public static IServiceCollection AddPluginHost(
        this IServiceCollection services,
        IEnumerable<Assembly> pluginAssemblies,
        string? pluginsDirectory = null,
        ILoggerFactory? loggerFactory = null,
        Version? hostApiVersion = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(pluginAssemblies);

        var logger = loggerFactory?.CreateLogger("ApiTestingStudio.Core.PluginHost");
        var loader = new PluginLoader(loggerFactory?.CreateLogger<PluginLoader>());
        var compatibility = new PluginCompatibilityChecker(hostApiVersion);

        var descriptors = new List<PluginDescriptor>();
        var runtimeEntries = new List<PluginRuntimeEntry>();

        RegisterCompileTimePlugins(services, loader, pluginAssemblies, descriptors, runtimeEntries, logger);
        RegisterDirectoryPlugins(services, loader, compatibility, pluginsDirectory, loggerFactory, descriptors, runtimeEntries, logger);

        var loaded = descriptors.Count(d => d.State == PluginLifecycleState.Loaded);
        var quarantined = descriptors.Count - loaded;
        logger?.LogInformation(
            "Plugin host registered {Total} plugin(s): {Loaded} loaded, {Quarantined} quarantined.",
            descriptors.Count, loaded, quarantined);

        services.AddSingleton<IPluginRegistry>(new PluginRegistry(descriptors));
        services.AddSingleton(new PluginLifecycleManager(runtimeEntries, loggerFactory?.CreateLogger<PluginLifecycleManager>()));
        services.AddHostedService<PluginLifecycleHostedService>();
        return services;
    }

    private static void RegisterCompileTimePlugins(
        IServiceCollection services,
        PluginLoader loader,
        IEnumerable<Assembly> pluginAssemblies,
        List<PluginDescriptor> descriptors,
        List<PluginRuntimeEntry> runtimeEntries,
        ILogger? logger)
    {
        foreach (var module in loader.Discover(pluginAssemblies))
        {
            RegisterModule(services, module, PluginSource.CompileTime, module.Name, context: null,
                descriptors, runtimeEntries, logger);
        }
    }

    private static void RegisterDirectoryPlugins(
        IServiceCollection services,
        PluginLoader loader,
        PluginCompatibilityChecker compatibility,
        string? pluginsDirectory,
        ILoggerFactory? loggerFactory,
        List<PluginDescriptor> descriptors,
        List<PluginRuntimeEntry> runtimeEntries,
        ILogger? logger)
    {
        if (string.IsNullOrWhiteSpace(pluginsDirectory))
        {
            return;
        }

        var scanner = new PluginDirectoryScanner(logger: loggerFactory?.CreateLogger<PluginDirectoryScanner>());
        foreach (var scan in scanner.Scan(pluginsDirectory))
        {
            if (!scan.IsSuccess)
            {
                descriptors.Add(Quarantine(scan.Source, scan.Source, new Version(0, 0), scan.Error!, PluginSource.Directory));
                continue;
            }

            var manifest = scan.Candidate!.Manifest;

            var compatibilityResult = compatibility.Check(manifest);
            if (compatibilityResult.IsFailure)
            {
                logger?.LogWarning(
                    "Quarantined plugin '{Id}' v{Version}: incompatible with host API {HostApi} ({Reason}).",
                    manifest.Id, manifest.Version, compatibility.HostApiVersion, compatibilityResult.Error.Message);
                descriptors.Add(Quarantine(manifest.Name, manifest.Id, manifest.Version, compatibilityResult.Error, PluginSource.Directory));
                continue;
            }

            var loadResult = loader.Load(scan.Candidate);
            if (loadResult.IsFailure)
            {
                descriptors.Add(Quarantine(manifest.Name, manifest.Id, manifest.Version, loadResult.Error, PluginSource.Directory));
                continue;
            }

            var plugin = loadResult.Value;
            RegisterModule(services, plugin.Module, PluginSource.Directory, manifest.Id, plugin.Context,
                descriptors, runtimeEntries, logger);
        }
    }

    /// <summary>
    /// Lets a module register its services (diffing the collection to infer capabilities) and
    /// records it. If <c>ConfigureServices</c> throws, any partial registrations are rolled back,
    /// the plugin is quarantined, and a directory plugin's load context is unloaded.
    /// </summary>
    private static void RegisterModule(
        IServiceCollection services,
        IPluginModule module,
        PluginSource source,
        string id,
        PluginLoadContext? context,
        List<PluginDescriptor> descriptors,
        List<PluginRuntimeEntry> runtimeEntries,
        ILogger? logger)
    {
        var assemblyName = module.GetType().Assembly.GetName().Name ?? "unknown";
        var startIndex = services.Count;

        IReadOnlyList<PluginCapability> capabilities;
        try
        {
            module.ConfigureServices(services);
            capabilities = PluginCapabilityMap.Detect(services, startIndex);
        }
#pragma warning disable CA1031 // ConfigureServices is a sanctioned boundary: a plugin fault must not crash composition.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            while (services.Count > startIndex)
            {
                services.RemoveAt(services.Count - 1);
            }

            context?.Unload();
            logger?.LogError(ex, "Quarantined plugin '{Id}': ConfigureServices threw.", id);
            descriptors.Add(new PluginDescriptor(
                module.Name, module.Version, assemblyName, source,
                PluginLifecycleState.Quarantined, [], id, new Error(ConfigureFailedCode, ex.Message)));
            return;
        }

        descriptors.Add(new PluginDescriptor(
            module.Name, module.Version, assemblyName, source,
            PluginLifecycleState.Loaded, capabilities, id));
        runtimeEntries.Add(new PluginRuntimeEntry
        {
            Id = id,
            Name = module.Name,
            Module = module,
            Context = context,
            Lifecycle = module as IPluginLifecycle,
            State = PluginLifecycleState.Loaded,
        });
    }

    private static PluginDescriptor Quarantine(string name, string id, Version version, Error error, PluginSource source) =>
        new(name, version, "unknown", source, PluginLifecycleState.Quarantined, [], id, error);
}
