using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Shared.Results;
using ApiTestingStudio.Shared.Versioning;

namespace ApiTestingStudio.Core.Plugins;

/// <summary>
/// Gates a plugin against the host plugin API version. A plugin declares the host range it supports
/// via its manifest; this checker rejects anything outside that range with a typed reason.
/// </summary>
public sealed class PluginCompatibilityChecker
{
    private readonly Version _hostApiVersion;

    /// <summary>Creates a checker for the given host API version, defaulting to <see cref="PluginApiVersion.Current"/>.</summary>
    public PluginCompatibilityChecker(Version? hostApiVersion = null)
        => _hostApiVersion = hostApiVersion ?? PluginApiVersion.Current;

    /// <summary>The host plugin API version this checker validates against.</summary>
    public Version HostApiVersion => _hostApiVersion;

    /// <summary>
    /// Returns success when the host API version falls within the manifest's supported range.
    /// On failure the <see cref="Error"/> explains which bound was violated.
    /// </summary>
    public Result Check(PluginManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return VersionCompatibility.Check(_hostApiVersion, manifest.MinHostApiVersion, manifest.MaxHostApiVersion);
    }
}
