using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Import;

/// <summary>
/// Typed, transport-agnostic errors for the import pipeline (detect → fetch → parse → merge).
/// Returned via <see cref="Result"/> so failures stay explicit and testable.
/// </summary>
public static class ImportErrors
{
    public static Error NoWorkspaceOpen { get; } =
        new("import.no_workspace", "No workspace is currently open.");

    public static Error NothingToImport { get; } =
        new("import.nothing_to_import", "Provide pasted text, a file, or a URL to import.");

    public static Error UnknownFormat { get; } =
        new("import.unknown_format", "Could not determine the format of the source, or no importer supports it.");

    public static Error NothingFound { get; } =
        new("import.nothing_found", "The source parsed successfully but contained no importable endpoints.");

    public static Error ParseFailed(string reason) =>
        new("import.parse_failed", $"The source could not be parsed: {reason}");

    public static Error FetchFailed(string reason) =>
        new("import.fetch_failed", $"The definition URL could not be fetched: {reason}");

    public static Error MergeFailed(string reason) =>
        new("import.merge_failed", $"The import could not be committed and was rolled back: {reason}");
}
