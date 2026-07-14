namespace ApiTestingStudio.Domain.Entities;

/// <summary>
/// Records a plugin/package a workspace depends on, so the workspace can declare which
/// capabilities are required to open it fully. Rows live in the workspace's own database and
/// are written when a plugin first contributes data to the workspace.
/// </summary>
public sealed record PackageMetadata
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkspaceId { get; init; }

    /// <summary>Stable plugin identifier (matches <c>IPluginModule.Name</c>).</summary>
    public required string PluginId { get; init; }

    /// <summary>Human-readable plugin name at the time it was recorded.</summary>
    public required string PluginName { get; init; }

    /// <summary>Plugin version string recorded when the dependency was captured.</summary>
    public required string Version { get; init; }

    public DateTimeOffset InstalledUtc { get; init; }
}
