using ApiTestingStudio.Application.Abstractions;
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

        return services;
    }
}
