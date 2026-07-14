using Microsoft.Extensions.Hosting;

namespace ApiTestingStudio.Core.Plugins;

/// <summary>
/// Bridges the host lifecycle to the <see cref="PluginLifecycleManager"/>: starts plugins when the
/// application starts and stops/unloads them on shutdown.
/// </summary>
internal sealed class PluginLifecycleHostedService : IHostedService
{
    private readonly PluginLifecycleManager _manager;

    public PluginLifecycleHostedService(PluginLifecycleManager manager) => _manager = manager;

    public Task StartAsync(CancellationToken cancellationToken) => _manager.StartAllAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => _manager.StopAllAsync(cancellationToken);
}
