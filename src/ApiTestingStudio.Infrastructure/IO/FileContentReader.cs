using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Infrastructure.IO;

/// <summary>
/// Filesystem-backed <see cref="IFileContentReader"/>. Reads UTF-8 text and maps the expected
/// I/O failures (missing/locked file, no access) to a typed <see cref="Error"/> rather than throwing.
/// </summary>
public sealed class FileContentReader : IFileContentReader
{
    public async Task<Result<string>> ReadTextAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Result.Failure<string>(new Error("file.read.empty_path", "No file path was provided."));
        }

        try
        {
            var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            return Result.Success(content);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result.Failure<string>(new Error("file.read.failed", $"Could not read the file: {ex.Message}"));
        }
    }
}
