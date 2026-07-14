namespace ApiTestingStudio.Plugin.Abstractions;

/// <summary>
/// Optional lifecycle hooks a plugin module may implement in addition to
/// <see cref="IPluginModule"/>. When a discovered module also implements this interface, the host's
/// lifecycle manager invokes these hooks as the plugin transitions through its states.
/// </summary>
/// <remarks>
/// This is deliberately <b>optional</b>: <see cref="IPluginModule"/> remains the only required
/// entry point, so plugins that just register services need not implement lifecycle. Hooks are
/// invoked in order: <see cref="InitializeAsync"/> after load, <see cref="StartAsync"/> once the
/// host is ready, and <see cref="StopAsync"/> during shutdown or unload. Implementations must be
/// idempotent-tolerant and must not block. See
/// <c>.claude/DECISIONS/ADR-0007-Dynamic-Plugin-Loading.md</c>.
/// </remarks>
public interface IPluginLifecycle
{
    /// <summary>Called once after the plugin is loaded, before it is started.</summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>Called once when the host is ready for the plugin to begin work.</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Called once during shutdown or unload to release resources.</summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
