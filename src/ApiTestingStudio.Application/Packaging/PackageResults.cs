namespace ApiTestingStudio.Application.Packaging;

/// <summary>The outcome of exporting the open workspace to an <c>.apistudio</c> package.</summary>
public sealed record PackageExportResult(string PackagePath, long SizeBytes);

/// <summary>
/// The outcome of importing/restoring an <c>.apistudio</c> package. <see cref="SecretsNeedReprompt"/>
/// is true when the package's secrets were bound to a different machine/user and therefore cannot be
/// decrypted here — the affected secrets must be re-entered (ADR-0012). <see cref="MissingPlugins"/>
/// lists declared plugin dependencies not currently loaded (non-fatal).
/// </summary>
public sealed record PackageImportResult(
    Guid WorkspaceId,
    string Location,
    bool SecretsNeedReprompt,
    IReadOnlyList<string> MissingPlugins);
