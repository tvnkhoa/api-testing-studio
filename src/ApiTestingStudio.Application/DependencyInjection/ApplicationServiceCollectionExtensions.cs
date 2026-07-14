using Microsoft.Extensions.DependencyInjection;

namespace ApiTestingStudio.Application.DependencyInjection;

/// <summary>
/// Registers application-layer services (use cases, handlers). Phase 1 has no concrete use
/// cases yet; this seam exists so the composition root can call <c>AddApplication()</c> today
/// and later sprints add registrations here without changing the host wiring.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Application use-case registrations are added here in later sprints.
        return services;
    }
}
