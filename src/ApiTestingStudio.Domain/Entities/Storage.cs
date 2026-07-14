namespace ApiTestingStudio.Domain.Entities;

/// <summary>
/// A reference to a binary attachment. The database stores only metadata; the bytes live
/// under the workspace <c>attachments/</c> folder addressed by <see cref="RelativePath"/>.
/// </summary>
public sealed record Attachment
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkspaceId { get; init; }

    public required string FileName { get; init; }

    public required string RelativePath { get; init; }

    public string? ContentType { get; init; }

    public long SizeBytes { get; init; }
}

/// <summary>A key/value setting scoped to a workspace.</summary>
public sealed record WorkspaceSetting
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkspaceId { get; init; }

    public required string Key { get; init; }

    public string? Value { get; init; }
}
