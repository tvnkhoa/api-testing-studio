using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Application-level use cases over the workspace lifecycle: create, open, close and delete a
/// workspace. Exactly one workspace is open at a time; creating or opening a workspace closes the
/// current one first. The currently open workspace is observable via <see cref="IWorkspaceSession"/>.
/// </summary>
public interface IWorkspaceService
{
    /// <summary>
    /// Creates a new, empty workspace at <paramref name="location"/> and opens it. Fails if the
    /// name is blank or a workspace already exists at the location.
    /// </summary>
    Task<Result<Workspace>> CreateAsync(
        string location,
        string name,
        string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens an existing workspace at <paramref name="location"/>. Fails when the target is
    /// missing, locked, corrupt, or written by a newer schema than this build supports.
    /// </summary>
    Task<Result<Workspace>> OpenAsync(string location, CancellationToken cancellationToken = default);

    /// <summary>Closes the open workspace. Fails with <c>workspace.none_open</c> when none is open.</summary>
    Task<Result> CloseAsync(CancellationToken cancellationToken = default);

    /// <summary>Permanently deletes the workspace at <paramref name="location"/>, closing it first if open.</summary>
    Task<Result> DeleteAsync(string location, CancellationToken cancellationToken = default);
}
