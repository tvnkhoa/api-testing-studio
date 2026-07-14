using System.Reflection;
using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Shared.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiTestingStudio.Core.Plugins;

/// <summary>
/// Discovers <see cref="IPluginModule"/> implementations. It reflects over compile-time assemblies
/// (Phase 1) and loads directory plugins into isolated collectible load contexts (Sprint 03). This
/// is the ONLY place that turns assemblies into plugin instances; the core never references a
/// concrete plugin.
/// </summary>
public sealed class PluginLoader : IPluginLoader
{
    /// <summary>Error code when a module type cannot be found in a directory plugin's entry assembly.</summary>
    public const string ModuleNotFoundCode = "plugin.module_not_found";

    /// <summary>Error code when loading a directory plugin threw.</summary>
    public const string LoadFailedCode = "plugin.load_failed";

    private readonly ILogger<PluginLoader> _logger;

    public PluginLoader(ILogger<PluginLoader>? logger = null)
        => _logger = logger ?? NullLogger<PluginLoader>.Instance;

    /// <inheritdoc />
    public IReadOnlyList<IPluginModule> Discover(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var modules = new List<IPluginModule>();

        foreach (var assembly in assemblies.Distinct())
        {
            foreach (var type in GetLoadableTypes(assembly))
            {
                if (IsPluginModule(type) && Activator.CreateInstance(type) is IPluginModule module)
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

        return modules;
    }

    /// <inheritdoc />
    public Result<LoadedPlugin> Load(DirectoryPluginCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        PluginLoadContext? context = null;
        try
        {
            context = new PluginLoadContext(candidate.EntryAssemblyPath, candidate.Manifest.Id);
            var assembly = context.LoadFromAssemblyPath(candidate.EntryAssemblyPath);

            var moduleType = ResolveModuleType(assembly, candidate.Manifest);
            if (moduleType is null)
            {
                context.Unload();
                return Result.Failure<LoadedPlugin>(new Error(
                    ModuleNotFoundCode,
                    $"No IPluginModule found in '{candidate.Manifest.EntryAssembly}'."));
            }

            if (Activator.CreateInstance(moduleType) is not IPluginModule module)
            {
                context.Unload();
                return Result.Failure<LoadedPlugin>(new Error(
                    ModuleNotFoundCode,
                    $"Type '{moduleType.FullName}' could not be instantiated as an IPluginModule."));
            }

            _logger.LogInformation(
                "Loaded plugin module {PluginName} v{PluginVersion} from directory plugin '{Id}'.",
                module.Name, module.Version, candidate.Manifest.Id);
            return Result.Success(new LoadedPlugin(module, context));
        }
#pragma warning disable CA1031 // Plugin load is a sanctioned boundary: any plugin failure must be isolated, not fatal.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            context?.Unload();
            _logger.LogError(ex, "Failed to load directory plugin '{Id}'.", candidate.Manifest.Id);
            return Result.Failure<LoadedPlugin>(new Error(LoadFailedCode, ex.Message));
        }
    }

    private static Type? ResolveModuleType(Assembly assembly, PluginManifest manifest)
    {
        if (!string.IsNullOrWhiteSpace(manifest.EntryType))
        {
            return assembly.GetType(manifest.EntryType, throwOnError: false);
        }

        return GetLoadableTypes(assembly).FirstOrDefault(IsPluginModule);
    }

    private static bool IsPluginModule(Type type) =>
        type is { IsAbstract: false, IsInterface: false } && typeof(IPluginModule).IsAssignableFrom(type);

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
