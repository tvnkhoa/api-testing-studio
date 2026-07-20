namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Exposes the ids of the plugins currently loaded in the host, so orchestration can report (without
/// depending on the plugin host directly) which of a package's declared plugin dependencies are
/// missing on import. Implemented over the plugin registry in the Host/Core layer. See ADR-0012.
/// </summary>
public interface IInstalledPluginCatalog
{
    /// <summary>Ids of successfully loaded plugins (matches <c>PackageMetadata.PluginId</c>).</summary>
    IReadOnlyCollection<string> InstalledPluginIds { get; }
}
