namespace ApiTestingStudio.Application.Backup;

/// <summary>
/// A single versioned backup archive (a timestamped <c>.apistudio</c> package) for a workspace,
/// stored under the app-data backups folder. See ADR-0012.
/// </summary>
public sealed record BackupEntry(
    string FilePath,
    Guid WorkspaceId,
    string WorkspaceName,
    DateTimeOffset CreatedUtc,
    long SizeBytes);
