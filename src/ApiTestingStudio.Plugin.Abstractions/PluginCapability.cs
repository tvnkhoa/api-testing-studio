namespace ApiTestingStudio.Plugin.Abstractions;

/// <summary>
/// A category of contract a plugin contributes. The host infers a plugin's capabilities from the
/// services it registers (see <c>AddPluginHost</c>) so the registry can be queried by category —
/// e.g. "give me every importer plugin". A single plugin may contribute several capabilities.
/// </summary>
public enum PluginCapability
{
    /// <summary>Contributes an <c>IImporter</c>.</summary>
    Importer,

    /// <summary>Contributes an <c>IExporter</c>.</summary>
    Exporter,

    /// <summary>Contributes an <c>IWorkspaceSerializer</c>.</summary>
    WorkspaceSerializer,

    /// <summary>Contributes an <c>IAssertion</c>.</summary>
    Assertion,

    /// <summary>Contributes an <c>IWorkflowNode</c>.</summary>
    WorkflowNode,

    /// <summary>Contributes an <c>IStressRunner</c>.</summary>
    StressRunner,

    /// <summary>Contributes an <c>IDashboardWidget</c>.</summary>
    DashboardWidget,

    /// <summary>Contributes an <c>IToolWindow</c>.</summary>
    ToolWindow,
}
