using System.IO;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Infrastructure.Persistence;
using ApiTestingStudio.Infrastructure.Security;
using ApiTestingStudio.Infrastructure.Settings;
using ApiTestingStudio.Infrastructure.Time;
using ApiTestingStudio.Plugin.Abstractions.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiTestingStudio.Infrastructure.DependencyInjection;

/// <summary>
/// Registers infrastructure adapters (persistence, security, time) behind their application/
/// plugin ports. Only the composition root (Host) calls this; UI/Core never reference it.
///
/// <para>No fixed database connection string is registered: the open workspace file is chosen at
/// runtime, so contexts are created on demand by <see cref="WorkspaceContextFactory"/> from the
/// <see cref="WorkspaceSession"/>. The MRU store lives under the app data directory.</para>
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    private const string RecentWorkspacesFileName = "recent-workspaces.json";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string appDataDirectory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(appDataDirectory);

        // Single open workspace, shared read-only view + on-demand context creation.
        services.AddSingleton<WorkspaceSession>();
        services.AddSingleton<IWorkspaceSession>(sp => sp.GetRequiredService<WorkspaceSession>());

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ISecretProtector, PlaceholderSecretProtector>();
        services.AddSingleton<IStorageProvider, SqliteStorageProvider>();
        services.AddSingleton<IPackageMetadataRepository, PackageMetadataRepository>();

        var recentStorePath = Path.Combine(appDataDirectory, RecentWorkspacesFileName);
        services.AddSingleton<IRecentWorkspacesService>(sp =>
            new RecentWorkspacesService(
                recentStorePath,
                sp.GetRequiredService<ILogger<RecentWorkspacesService>>()));

        return services;
    }
}
