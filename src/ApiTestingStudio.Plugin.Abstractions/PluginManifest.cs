namespace ApiTestingStudio.Plugin.Abstractions;

/// <summary>
/// Metadata a directory-loaded plugin declares in its <c>manifest.json</c>. The host reads this
/// before loading any code so it can gate compatibility and locate the entry assembly.
/// </summary>
/// <remarks>
/// Compile-time (first-party) plugins do not require a manifest; the manifest is the contract for
/// drop-in <c>plugins/</c> directory plugins. See
/// <c>.claude/DECISIONS/ADR-0007-Dynamic-Plugin-Loading.md</c>.
/// </remarks>
/// <param name="Id">Stable unique identifier (e.g. "Sample.HelloWorld").</param>
/// <param name="Name">Human-readable plugin name.</param>
/// <param name="Version">The plugin's own version.</param>
/// <param name="EntryAssembly">File name of the plugin's main assembly, relative to its folder.</param>
/// <param name="MinHostApiVersion">Minimum host <see cref="PluginApiVersion"/> the plugin supports.</param>
/// <param name="MaxHostApiVersion">Maximum host API version, or <c>null</c> for no upper bound.</param>
/// <param name="EntryType">
/// Optional fully-qualified <see cref="IPluginModule"/> type name. When omitted, the host reflects
/// over the entry assembly to find the module.
/// </param>
/// <param name="Description">Optional human-readable description.</param>
public sealed record PluginManifest(
    string Id,
    string Name,
    Version Version,
    string EntryAssembly,
    Version MinHostApiVersion,
    Version? MaxHostApiVersion = null,
    string? EntryType = null,
    string? Description = null);
