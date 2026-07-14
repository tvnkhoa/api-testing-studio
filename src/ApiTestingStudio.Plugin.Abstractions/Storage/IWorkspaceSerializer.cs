namespace ApiTestingStudio.Plugin.Abstractions.Storage;

/// <summary>
/// Reads and writes the portable <c>.apistudio</c> package
/// (manifest.json + database.sqlite + attachments/). Implemented by <c>Export.ApiStudio</c>.
/// </summary>
public interface IWorkspaceSerializer
{
    /// <summary>The package format handled (e.g. "apistudio").</summary>
    string Format { get; }

    /// <summary>Writes the workspace to a package at <paramref name="packagePath"/>.</summary>
    Task SaveAsync(Guid workspaceId, string packagePath, CancellationToken cancellationToken = default);

    /// <summary>Reads a package and returns the imported workspace id.</summary>
    Task<Guid> LoadAsync(string packagePath, CancellationToken cancellationToken = default);
}
