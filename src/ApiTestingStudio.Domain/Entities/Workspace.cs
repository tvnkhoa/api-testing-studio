namespace ApiTestingStudio.Domain.Entities;

/// <summary>
/// The aggregate root. Everything in the application belongs to exactly one workspace.
/// Child entities reference their owner via <c>WorkspaceId</c> foreign keys rather than
/// embedded collections, keeping the model flat and persistence-friendly.
/// </summary>
public sealed record Workspace
{
    /// <summary>
    /// The workspace schema version this build of the application creates and understands.
    /// Opening a workspace whose <see cref="SchemaVersion"/> is greater than this must fail
    /// safely — the file was written by a newer app. See <c>.claude/DATABASE_GUIDELINES.md</c>.
    /// </summary>
    public const int CurrentSchemaVersion = 6;

    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; init; }

    public string? Description { get; init; }

    /// <summary>Schema version of the workspace, used for forward-compatible migrations.</summary>
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public DateTimeOffset CreatedUtc { get; init; }

    public DateTimeOffset ModifiedUtc { get; init; }
}
