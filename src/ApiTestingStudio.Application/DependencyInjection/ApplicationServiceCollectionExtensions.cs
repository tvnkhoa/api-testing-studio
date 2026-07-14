using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.ServiceCatalog;
using ApiTestingStudio.Application.Workspaces;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTestingStudio.Application.DependencyInjection;

/// <summary>
/// Registers application-layer use cases. Ports whose implementations live in Infrastructure
/// (storage, recent-workspaces, clock) are bound by <c>AddInfrastructure</c>; this method wires
/// only the pure application services.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IWorkspaceService, WorkspaceService>();

        // Service Explorer (Sprint 05): tree read + service/folder CRUD, endpoint CRUD, per-workspace
        // tree state. These depend on the catalog repositories bound by AddInfrastructure.
        services.AddSingleton<IServiceExplorerService, ServiceExplorerService>();
        services.AddSingleton<IEndpointCrudService, EndpointCrudService>();
        services.AddSingleton<IServiceExplorerStateService, ServiceExplorerStateService>();

        return services;
    }
}
