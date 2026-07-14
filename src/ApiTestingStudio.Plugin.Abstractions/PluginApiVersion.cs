namespace ApiTestingStudio.Plugin.Abstractions;

/// <summary>
/// The version of the plugin contract surface exposed by this assembly. Plugins declare the host
/// API range they support (see <see cref="PluginManifest"/>); the host gates loading against
/// <see cref="Current"/>.
/// </summary>
/// <remarks>
/// This is a SemVer-governed public contract. Breaking any contract in
/// <c>ApiTestingStudio.Plugin.Abstractions</c> is an ADR-level decision and must bump
/// <see cref="Current"/>. See <c>.claude/DECISIONS/ADR-0007-Dynamic-Plugin-Loading.md</c>.
/// </remarks>
public static class PluginApiVersion
{
    /// <summary>The current host plugin API version.</summary>
    public static readonly Version Current = new(1, 0, 0);
}
