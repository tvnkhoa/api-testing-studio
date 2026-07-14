namespace ApiTestingStudio.Domain.Entities;

/// <summary>
/// The aggregate root. Everything in the application belongs to exactly one workspace.
/// Child entities reference their owner via <c>WorkspaceId</c> foreign keys rather than
/// embedded collections, keeping the model flat and persistence-friendly.
/// </summary>
public sealed record Workspace
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; init; }

    public string? Description { get; init; }

    /// <summary>Schema version of the workspace, used for forward-compatible migrations.</summary>
    public int SchemaVersion { get; init; } = 1;

    public DateTimeOffset CreatedUtc { get; init; }

    public DateTimeOffset ModifiedUtc { get; init; }
}
