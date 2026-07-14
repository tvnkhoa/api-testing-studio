using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Plugin.Abstractions.Assertions;
using ApiTestingStudio.Plugin.Abstractions.Exporting;
using ApiTestingStudio.Plugin.Abstractions.Importing;
using ApiTestingStudio.Plugin.Abstractions.Runners;
using ApiTestingStudio.Plugin.Abstractions.Storage;
using ApiTestingStudio.Plugin.Abstractions.Ui;
using ApiTestingStudio.Plugin.Abstractions.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTestingStudio.Core.Plugins;

/// <summary>
/// Maps the contract service types a plugin registers to <see cref="PluginCapability"/> values.
/// The host diffs the service collection around each module's <c>ConfigureServices</c> call and
/// uses this map to infer which capabilities the plugin contributes — uniformly for compile-time
/// and directory plugins, with no manifest declaration required.
/// </summary>
internal static class PluginCapabilityMap
{
    private static readonly Dictionary<Type, PluginCapability> ContractToCapability =
        new()
        {
            [typeof(IImporter)] = PluginCapability.Importer,
            [typeof(IExporter)] = PluginCapability.Exporter,
            [typeof(IWorkspaceSerializer)] = PluginCapability.WorkspaceSerializer,
            [typeof(IAssertion)] = PluginCapability.Assertion,
            [typeof(IWorkflowNode)] = PluginCapability.WorkflowNode,
            [typeof(IStressRunner)] = PluginCapability.StressRunner,
            [typeof(IDashboardWidget)] = PluginCapability.DashboardWidget,
            [typeof(IToolWindow)] = PluginCapability.ToolWindow,
        };

    /// <summary>
    /// Inspects the service descriptors added to <paramref name="services"/> from
    /// <paramref name="startIndex"/> onward and returns the distinct capabilities they represent.
    /// </summary>
    public static IReadOnlyList<PluginCapability> Detect(IServiceCollection services, int startIndex)
    {
        var capabilities = new HashSet<PluginCapability>();
        for (var i = startIndex; i < services.Count; i++)
        {
            if (ContractToCapability.TryGetValue(services[i].ServiceType, out var capability))
            {
                capabilities.Add(capability);
            }
        }

        return [.. capabilities];
    }
}
