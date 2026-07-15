namespace ApiTestingStudio.Application.Import;

/// <summary>
/// Best-guess classification of an import source into an importer <c>Format</c> ("curl", "openapi",
/// "postman", "scalar") from pasted text, a file name, and/or a URL. Returns null when nothing
/// matches. Cheap and heuristic — the selected importer still confirms via <c>CanImport</c>.
/// </summary>
public interface ISourceFormatDetector
{
    string? Detect(string? content, string? fileName = null, string? uri = null);
}
