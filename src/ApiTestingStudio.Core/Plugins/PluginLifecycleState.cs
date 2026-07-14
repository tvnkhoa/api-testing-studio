namespace ApiTestingStudio.Core.Plugins;

/// <summary>
/// The lifecycle state of a plugin. The happy path runs
/// Discovered → Loaded → Initialized → Started → Stopped → Unloaded. A plugin that fails
/// compatibility, loading, or configuration is <see cref="Quarantined"/>; one that throws during a
/// lifecycle hook is marked <see cref="Failed"/>. See
/// <c>.claude/DECISIONS/ADR-0007-Dynamic-Plugin-Loading.md</c>.
/// </summary>
public enum PluginLifecycleState
{
    /// <summary>Found (manifest read / module reflected) but not yet loaded into a context.</summary>
    Discovered,

    /// <summary>Module instantiated and its services registered.</summary>
    Loaded,

    /// <summary>Optional <c>IPluginLifecycle.InitializeAsync</c> completed.</summary>
    Initialized,

    /// <summary>Optional <c>IPluginLifecycle.StartAsync</c> completed (or no lifecycle to run).</summary>
    Started,

    /// <summary>Optional <c>IPluginLifecycle.StopAsync</c> completed.</summary>
    Stopped,

    /// <summary>The plugin's load context has been unloaded.</summary>
    Unloaded,

    /// <summary>Rejected before or during load (incompatible, unreadable, or bad configuration).</summary>
    Quarantined,

    /// <summary>Threw during a lifecycle hook; isolated so the host keeps running.</summary>
    Failed,
}
