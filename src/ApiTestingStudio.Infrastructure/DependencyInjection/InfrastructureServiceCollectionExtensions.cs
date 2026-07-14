using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Infrastructure.Persistence;
using ApiTestingStudio.Infrastructure.Security;
using ApiTestingStudio.Infrastructure.Time;
using ApiTestingStudio.Plugin.Abstractions.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTestingStudio.Infrastructure.DependencyInjection;

/// <summary>
/// Registers infrastructure adapters (persistence, security, time) behind their application/
/// plugin ports. Only the composition root (Host) calls this; UI/Core never reference it.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string sqliteConnectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(sqliteConnectionString);

        services.AddDbContext<WorkspaceDbContext>(options =>
            options.UseSqlite(sqliteConnectionString));

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ISecretProtector, PlaceholderSecretProtector>();
        services.AddScoped<IStorageProvider, SqliteStorageProvider>();

        return services;
    }
}
