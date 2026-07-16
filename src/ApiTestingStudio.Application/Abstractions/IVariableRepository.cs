using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Persists <see cref="Variable"/> rows for the currently open workspace. Secret variables store
/// their <c>Value</c> as ciphertext. EF Core types never cross this port.
/// </summary>
public interface IVariableRepository
{
    /// <summary>Returns every variable in the workspace, ordered by scope then key.</summary>
    Task<IReadOnlyList<Variable>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>Returns the workspace's variables in the given scope.</summary>
    Task<IReadOnlyList<Variable>> GetByScopeAsync(Guid workspaceId, VariableScope scope, CancellationToken cancellationToken = default);

    /// <summary>Returns the variables bound to a specific environment.</summary>
    Task<IReadOnlyList<Variable>> GetByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken = default);

    /// <summary>Returns the variable with the given id, or null.</summary>
    Task<Variable?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Inserts a new variable.</summary>
    Task AddAsync(Variable variable, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing variable.</summary>
    Task UpdateAsync(Variable variable, CancellationToken cancellationToken = default);

    /// <summary>Deletes the variable with the given id.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
