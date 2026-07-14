namespace ApiTestingStudio.Application.Workspaces;

/// <summary>
/// One entry in the recently-opened-workspaces (MRU) list. Persisted outside any workspace
/// database (in the app settings store) so it survives across workspaces and app restarts.
/// </summary>
public sealed record RecentWorkspaceEntry
{
    /// <summary>Provider-specific locator of the workspace (a file path for the SQLite provider).</summary>
    public required string Location { get; init; }

    /// <summary>Workspace display name captured when it was last opened.</summary>
    public required string Name { get; init; }

    /// <summary>When the workspace was last created or opened.</summary>
    public DateTimeOffset LastOpenedUtc { get; init; }
}
