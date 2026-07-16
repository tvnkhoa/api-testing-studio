using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Environments;

/// <summary>
/// Create/read/update/delete environments for the open workspace and track which environment is
/// active. The active selection is persisted per-workspace in the Settings table (no schema change).
/// </summary>
public interface IEnvironmentService
{
    Task<Result<IReadOnlyList<EnvironmentDefinition>>> ListAsync(CancellationToken cancellationToken = default);

    Task<Result<EnvironmentDefinition>> CreateAsync(string name, EnvironmentKind kind, CancellationToken cancellationToken = default);

    Task<Result<EnvironmentDefinition>> UpdateAsync(Guid id, string name, EnvironmentKind kind, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the active environment id for the open workspace, or null.</summary>
    Task<Guid?> GetActiveIdAsync(CancellationToken cancellationToken = default);

    /// <summary>Sets (or clears, when null) the active environment for the open workspace.</summary>
    Task<Result> SetActiveAsync(Guid? environmentId, CancellationToken cancellationToken = default);
}
