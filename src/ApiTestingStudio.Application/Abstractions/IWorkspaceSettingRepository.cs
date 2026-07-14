namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Reads and writes per-workspace key/value settings stored inside the currently open workspace's
/// database (the <c>Settings</c> table). Distinct from the app-global preference stores, this state
/// travels with the workspace file. Operations require an open workspace.
/// See <c>.claude/DATABASE_GUIDELINES.md</c>.
/// </summary>
public interface IWorkspaceSettingRepository
{
    /// <summary>Returns the value stored for <paramref name="key"/>, or null when absent.</summary>
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Inserts or updates the value for <paramref name="key"/>.</summary>
    Task SetAsync(string key, string? value, CancellationToken cancellationToken = default);
}
