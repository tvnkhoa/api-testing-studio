using System.IO;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Import;
using ApiTestingStudio.Infrastructure.Http;
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
    private const string AppSettingsFileName = "app-settings.json";
    private const string LayoutFileName = "dock-layout.xml";

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

        // Service Explorer catalog repositories (Sprint 05). Short-lived contexts per op, keyed off
        // the open workspace's connection string held by the session.
        services.AddSingleton<IServiceRepository, ServiceRepository>();
        services.AddSingleton<IEndpointFolderRepository, EndpointFolderRepository>();
        services.AddSingleton<IEndpointRepository, EndpointRepository>();
        services.AddSingleton<IWorkspaceSettingRepository, WorkspaceSettingRepository>();

        // API Runner (Sprint 06): HTTP execution engine (single long-lived handler) + history store.
        services.AddSingleton<IRequestExecutor, HttpRequestExecutor>();
        services.AddSingleton<IRequestHistoryRepository, RequestHistoryRepository>();

        // Import (Sprint 07): user-triggered URL fetch (offline-first) + transactional catalog merge.
        services.AddSingleton<IDefinitionFetcher, DefinitionFetcher>();
        services.AddSingleton<ICatalogMerger, CatalogMerger>();

        var recentStorePath = Path.Combine(appDataDirectory, RecentWorkspacesFileName);
        services.AddSingleton<IRecentWorkspacesService>(sp =>
            new RecentWorkspacesService(
                recentStorePath,
                sp.GetRequiredService<ILogger<RecentWorkspacesService>>()));

        // Shell preferences (theme) and docking layout live alongside the MRU store under app-data,
        // outside any workspace database, so they survive restarts and workspace switches.
        var appSettingsPath = Path.Combine(appDataDirectory, AppSettingsFileName);
        services.AddSingleton<IAppSettingsService>(sp =>
            new AppSettingsService(
                appSettingsPath,
                sp.GetRequiredService<ILogger<AppSettingsService>>()));

        var layoutPath = Path.Combine(appDataDirectory, LayoutFileName);
        services.AddSingleton<ILayoutPersistenceService>(sp =>
            new LayoutPersistenceService(
                layoutPath,
                sp.GetRequiredService<ILogger<LayoutPersistenceService>>()));

        return services;
    }
}
