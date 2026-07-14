using System.Reflection;
using ApiTestingStudio.Assertion.Json;
using ApiTestingStudio.Assertion.Regex;
using ApiTestingStudio.Assertion.Schema;
using ApiTestingStudio.Core.DependencyInjection;
using ApiTestingStudio.Core.Plugins;
using ApiTestingStudio.Export.ApiStudio;
using ApiTestingStudio.Import.Curl;
using ApiTestingStudio.Import.OpenApi;
using ApiTestingStudio.Import.Postman;
using ApiTestingStudio.Import.Scalar;
using ApiTestingStudio.Plugin.Abstractions.Assertions;
using ApiTestingStudio.Plugin.Abstractions.Exporting;
using ApiTestingStudio.Plugin.Abstractions.Importing;
using ApiTestingStudio.Plugin.Abstractions.Runners;
using ApiTestingStudio.Runner.Stress;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTestingStudio.PluginHost.Tests;

/// <summary>
/// Acceptance tests for Phase 1: the plugin host must discover every plugin module and each
/// module must register its contributed services into the container.
/// </summary>
public sealed class PluginDiscoveryTests
{
    private static readonly Assembly[] PluginAssemblies =
    [
        typeof(CurlImportPluginModule).Assembly,
        typeof(OpenApiImportPluginModule).Assembly,
        typeof(ScalarImportPluginModule).Assembly,
        typeof(PostmanImportPluginModule).Assembly,
        typeof(JsonAssertionPluginModule).Assembly,
        typeof(RegexAssertionPluginModule).Assembly,
        typeof(SchemaAssertionPluginModule).Assembly,
        typeof(StressRunnerPluginModule).Assembly,
        typeof(ApiStudioExportPluginModule).Assembly,
    ];

    [Fact]
    public void AddPluginHost_discovers_all_nine_plugin_modules()
    {
        var provider = BuildProvider();

        var registry = provider.GetRequiredService<IPluginRegistry>();

        registry.Plugins.Should().HaveCount(9);
        registry.Plugins.Select(p => p.Name).Should().Contain(
        [
            "Import.Curl", "Import.OpenApi", "Import.Scalar", "Import.Postman",
            "Assertion.Json", "Assertion.Regex", "Assertion.Schema",
            "Runner.Stress", "Export.ApiStudio",
        ]);
    }

    [Fact]
    public void AddPluginHost_registers_each_plugins_contributed_services()
    {
        var provider = BuildProvider();

        provider.GetServices<IImporter>().Should().HaveCount(4);
        provider.GetServices<IAssertion>().Should().HaveCount(3);
        provider.GetService<IStressRunner>().Should().NotBeNull();
        provider.GetService<IExporter>().Should().NotBeNull();
        provider.GetService<Plugin.Abstractions.Storage.IWorkspaceSerializer>().Should().NotBeNull();
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddPluginHost(PluginAssemblies);
        return services.BuildServiceProvider();
    }
}
