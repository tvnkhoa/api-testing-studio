namespace ApiTestingStudio.Plugin.Abstractions.Exporting;

/// <summary>A request to export a workspace to a package on disk.</summary>
public sealed record ExportRequest(Guid WorkspaceId, string TargetPath);

/// <summary>The outcome of an export operation.</summary>
public sealed record ExportResult(string PackagePath, long SizeBytes);

/// <summary>
/// Writes a workspace to an external package. Phase 1 supports only the <c>.apistudio</c>
/// format (implemented by <c>Export.ApiStudio</c>).
/// </summary>
public interface IExporter
{
    /// <summary>The package format this exporter produces (e.g. "apistudio").</summary>
    string Format { get; }

    /// <summary>Exports the workspace, returning the produced package.</summary>
    Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken = default);
}
