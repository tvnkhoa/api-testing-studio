namespace ApiTestingStudio.Plugin.Abstractions.Storage;

/// <summary>A plugin/package the workspace depended on when it was exported.</summary>
public sealed record PackagePluginDependency(string PluginId, string PluginName, string Version);

/// <summary>
/// Records how the packaged workspace's secrets are bound. Secrets are AES-256-GCM ciphertext inside
/// <c>database.sqlite</c>, keyed by a DPAPI-wrapped master key bound to the exporting user/machine
/// (ADR-0010). <see cref="KeyFingerprint"/> is a NON-reversible hash of that key (never the key
/// itself); on import a differing fingerprint means the ciphertext will not decrypt here and the
/// secrets must be re-entered. See ADR-0012.
/// </summary>
public sealed record SecretBinding(bool MachineBound, string? KeyFingerprint);

/// <summary>
/// The <c>manifest.json</c> at the root of an <c>.apistudio</c> package. Carries the version and
/// dependency bookkeeping needed to validate an import and to flag non-portable secrets. Derived at
/// export time — no domain table backs it. See <c>.claude/FEATURES/Packaging.md</c> and ADR-0012.
/// </summary>
public sealed record PackageManifest(
    string FormatVersion,
    int WorkspaceSchemaVersion,
    string AppVersion,
    Guid WorkspaceId,
    string WorkspaceName,
    DateTimeOffset CreatedUtc,
    IReadOnlyList<PackagePluginDependency> Plugins,
    SecretBinding Secrets)
{
    /// <summary>The package layout schema this build writes. A major-version bump is a breaking change.</summary>
    public const string CurrentFormatVersion = "1.0";
}
