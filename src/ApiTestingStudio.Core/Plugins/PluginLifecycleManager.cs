using ApiTestingStudio.Plugin.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiTestingStudio.Core.Plugins;

/// <summary>A read-only snapshot of one plugin's current lifecycle state.</summary>
/// <param name="Id">Plugin id (manifest id or module name).</param>
/// <param name="Name">Plugin module name.</param>
/// <param name="State">Current lifecycle state.</param>
public sealed record PluginLifecycleSnapshot(string Id, string Name, PluginLifecycleState State);

/// <summary>
/// Mutable runtime record for a loaded plugin the lifecycle manager drives. Holds references into a
/// plugin's load context, so it lives only inside the manager (never the registry).
/// </summary>
internal sealed class PluginRuntimeEntry
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required IPluginModule Module { get; init; }

    /// <summary>The isolated load context for a directory plugin; null for compile-time plugins.</summary>
    public PluginLoadContext? Context { get; set; }

    /// <summary>The optional lifecycle hooks the module implements, if any.</summary>
    public IPluginLifecycle? Lifecycle { get; init; }

    public PluginLifecycleState State { get; set; } = PluginLifecycleState.Loaded;
}

/// <summary>
/// Drives loaded plugins through their lifecycle: it initialises and starts them when the host
/// starts, and stops and unloads them on shutdown. A plugin that throws during a hook is isolated
/// (marked <see cref="PluginLifecycleState.Failed"/>) so the host and other plugins keep running.
/// </summary>
public sealed class PluginLifecycleManager
{
    private readonly List<PluginRuntimeEntry> _entries;
    private readonly ILogger<PluginLifecycleManager> _logger;

    internal PluginLifecycleManager(
        IReadOnlyList<PluginRuntimeEntry> entries,
        ILogger<PluginLifecycleManager>? logger = null)
    {
        _entries = [.. entries];
        _logger = logger ?? NullLogger<PluginLifecycleManager>.Instance;
    }

    /// <summary>Current lifecycle state of every managed plugin.</summary>
    public IReadOnlyList<PluginLifecycleSnapshot> Snapshots =>
        _entries.Select(e => new PluginLifecycleSnapshot(e.Id, e.Name, e.State)).ToList();

    /// <summary>Initialises and starts every loaded plugin, running optional lifecycle hooks.</summary>
    public async Task StartAllAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in _entries.Where(e => e.State == PluginLifecycleState.Loaded))
        {
            try
            {
                if (entry.Lifecycle is not null)
                {
                    await entry.Lifecycle.InitializeAsync(cancellationToken).ConfigureAwait(false);
                    Transition(entry, PluginLifecycleState.Initialized);

                    await entry.Lifecycle.StartAsync(cancellationToken).ConfigureAwait(false);
                }

                Transition(entry, PluginLifecycleState.Started);
            }
#pragma warning disable CA1031 // Lifecycle is a sanctioned boundary: a plugin fault must not crash the host.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                Transition(entry, PluginLifecycleState.Failed);
                _logger.LogError(ex, "Plugin '{Id}' failed during startup and was isolated.", entry.Id);
            }
        }
    }

    /// <summary>Stops every running plugin (in reverse order) and unloads directory load contexts.</summary>
    public async Task StopAllAsync(CancellationToken cancellationToken = default)
    {
        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];
            if (entry.State is not (PluginLifecycleState.Started or PluginLifecycleState.Initialized or PluginLifecycleState.Failed))
            {
                continue;
            }

            try
            {
                if (entry.Lifecycle is not null)
                {
                    await entry.Lifecycle.StopAsync(cancellationToken).ConfigureAwait(false);
                }

                Transition(entry, PluginLifecycleState.Stopped);
            }
#pragma warning disable CA1031 // Lifecycle is a sanctioned boundary: a plugin fault must not crash shutdown.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.LogError(ex, "Plugin '{Id}' threw during shutdown.", entry.Id);
            }
        }

        foreach (var entry in _entries.Where(e => e.Context is not null))
        {
            entry.Context!.Unload();
            entry.Context = null;
            Transition(entry, PluginLifecycleState.Unloaded);
        }
    }

    private void Transition(PluginRuntimeEntry entry, PluginLifecycleState state)
    {
        entry.State = state;
        _logger.LogInformation("Plugin '{Id}' -> {State}.", entry.Id, state);
    }
}
