using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using ApiTestingStudio.Core.DependencyInjection;
using ApiTestingStudio.Core.Plugins;
using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Plugin.Abstractions.Ui;
using ApiTestingStudio.Shared.Versioning;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTestingStudio.PluginHost.Tests;

/// <summary>
/// Sprint 03 acceptance tests for the directory-based dynamic plugin system: dynamic load via
/// AssemblyLoadContext, version-compatibility quarantine, throwing-plugin quarantine, collectible
/// unload, and capability queries.
/// </summary>
public sealed class DynamicPluginLoadingTests : IDisposable
{
    private const string SampleId = "Sample.HelloWorld";
    private const string SampleModuleType = "ApiTestingStudio.Sample.HelloWorld.HelloWorldPluginModule";
    private const string ThrowingModuleType = "ApiTestingStudio.Sample.HelloWorld.ThrowingPluginModule";

    private static readonly string DeployedPluginsDirectory = Path.Combine(AppContext.BaseDirectory, "plugins");
    private static readonly string SampleAssemblyPath =
        Path.Combine(DeployedPluginsDirectory, SampleId, "ApiTestingStudio.Sample.HelloWorld.dll");

    private readonly List<string> _tempDirectories = [];

    [Fact]
    public void AddPluginHost_loads_sample_plugin_from_directory()
    {
        var provider = BuildProvider(DeployedPluginsDirectory);
        var registry = provider.GetRequiredService<IPluginRegistry>();

        var sample = registry.Plugins.SingleOrDefault(p => p.Id == SampleId);

        sample.Should().NotBeNull();
        sample!.State.Should().Be(PluginLifecycleState.Loaded);
        sample.Source.Should().Be(PluginSource.Directory);
        sample.Version.Should().Be(new Version(1, 0, 0));
    }

    [Fact]
    public void Directory_plugin_registers_its_contributed_service()
    {
        var provider = BuildProvider(DeployedPluginsDirectory);

        provider.GetService<IToolWindow>().Should().NotBeNull();
    }

    [Fact]
    public void Registry_filters_plugins_by_capability()
    {
        var provider = BuildProvider(DeployedPluginsDirectory);
        var registry = provider.GetRequiredService<IPluginRegistry>();

        var toolWindowPlugins = registry.GetByCapability(PluginCapability.ToolWindow);

        toolWindowPlugins.Should().ContainSingle(p => p.Id == SampleId);
        registry.GetByCapability(PluginCapability.StressRunner).Should().NotContain(p => p.Id == SampleId);
    }

    [Fact]
    public void Incompatible_plugin_is_quarantined_with_typed_reason()
    {
        var directory = CreateTempPluginDirectory(Manifest(SampleModuleType, minHostApiVersion: "99.0.0"));

        var provider = BuildProvider(directory);
        var registry = provider.GetRequiredService<IPluginRegistry>();

        var sample = registry.Plugins.Single(p => p.Id == SampleId);
        sample.State.Should().Be(PluginLifecycleState.Quarantined);
        sample.Error.Should().NotBeNull();
        sample.Error!.Code.Should().Be(VersionCompatibility.BelowMinimumCode);
        provider.GetService<IToolWindow>().Should().BeNull("a quarantined plugin must not register services");
    }

    [Fact]
    public void Throwing_plugin_is_quarantined_and_does_not_crash_the_host()
    {
        var directory = CreateTempPluginDirectory(Manifest(ThrowingModuleType, minHostApiVersion: "1.0.0"));

        // Building the host with a throwing plugin must not throw.
        var provider = BuildProvider(directory);
        var registry = provider.GetRequiredService<IPluginRegistry>();

        var faulty = registry.Plugins.Single();
        faulty.State.Should().Be(PluginLifecycleState.Quarantined);
        faulty.Error!.Code.Should().Be(PluginHostServiceCollectionExtensions.ConfigureFailedCode);
        provider.GetService<IToolWindow>().Should().BeNull("a throwing plugin's partial registrations must be rolled back");
    }

    [Fact]
    public async Task LifecycleManager_starts_and_stops_directory_plugin()
    {
        var provider = BuildProvider(DeployedPluginsDirectory);
        var manager = provider.GetRequiredService<PluginLifecycleManager>();

        await manager.StartAllAsync(CancellationToken.None);
        manager.Snapshots.Single(s => s.Id == SampleId).State.Should().Be(PluginLifecycleState.Started);

        await manager.StopAllAsync(CancellationToken.None);
        manager.Snapshots.Single(s => s.Id == SampleId).State.Should().Be(PluginLifecycleState.Unloaded);
    }

    [Fact]
    public void PluginLoadContext_is_collectible_and_unloads()
    {
        var weakReference = LoadThenUnload(SampleAssemblyPath);

        for (var i = 0; weakReference.IsAlive && i < 10; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        weakReference.IsAlive.Should().BeFalse("a collectible load context must be collected after Unload");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference LoadThenUnload(string assemblyPath)
    {
        var context = new PluginLoadContext(assemblyPath, "unload-test");
        var assembly = context.LoadFromAssemblyPath(assemblyPath);
        _ = assembly.GetName();

        var weakReference = new WeakReference(context);
        context.Unload();
        return weakReference;
    }

    private static ServiceProvider BuildProvider(string pluginsDirectory)
    {
        var services = new ServiceCollection();
        services.AddPluginHost(Array.Empty<Assembly>(), pluginsDirectory);
        return services.BuildServiceProvider();
    }

    private static string Manifest(string entryType, string minHostApiVersion) =>
        $$"""
        {
          "id": "{{SampleId}}",
          "name": "Sample",
          "version": "1.0.0",
          "entryAssembly": "ApiTestingStudio.Sample.HelloWorld.dll",
          "entryType": "{{entryType}}",
          "minHostApiVersion": "{{minHostApiVersion}}"
        }
        """;

    private string CreateTempPluginDirectory(string manifestJson)
    {
        var root = Path.Combine(Path.GetTempPath(), "ats-plugin-tests", Guid.NewGuid().ToString("N"));
        var pluginDirectory = Path.Combine(root, SampleId);
        Directory.CreateDirectory(pluginDirectory);
        _tempDirectories.Add(root);

        File.Copy(SampleAssemblyPath, Path.Combine(pluginDirectory, Path.GetFileName(SampleAssemblyPath)));
        var depsPath = Path.ChangeExtension(SampleAssemblyPath, ".deps.json");
        if (File.Exists(depsPath))
        {
            File.Copy(depsPath, Path.Combine(pluginDirectory, Path.GetFileName(depsPath)));
        }

        File.WriteAllText(Path.Combine(pluginDirectory, PluginManifestReader.ManifestFileName), manifestJson);
        return root;
    }

    public void Dispose()
    {
        foreach (var directory in _tempDirectories)
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
                // A load context may still hold the copied assembly; best-effort cleanup.
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup.
            }
        }
    }
}
