namespace ApiTestingStudio.Plugin.Abstractions.Exporting;

/// <summary>
/// Declares an export format a workspace can be packaged into, so the orchestrator and UI can offer
/// and select it. The actual byte I/O is done through <c>IWorkspaceSerializer</c>; orchestration
/// (DB maintenance, manifest, install) lives in the Application layer. Phase 1 ships only the native
/// <c>.apistudio</c> format (implemented by <c>Export.ApiStudio</c>); cross-format export is future
/// work. See ADR-0012.
/// </summary>
public interface IExporter
{
    /// <summary>Stable identifier for the format (e.g. "apistudio"). Matches the serializer's Format.</summary>
    string Format { get; }

    /// <summary>Human-readable name for menus/dialogs (e.g. "API Testing Studio Package").</summary>
    string DisplayName { get; }

    /// <summary>The package file extension including the dot (e.g. ".apistudio").</summary>
    string FileExtension { get; }
}
