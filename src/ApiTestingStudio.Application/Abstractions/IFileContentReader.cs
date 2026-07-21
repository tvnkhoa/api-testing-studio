using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Reads the text content of a local file. Keeps filesystem I/O out of view models and other callers
/// so they depend on this port rather than <c>System.IO.File</c> directly. Expected, recoverable
/// failures (missing file, no access, I/O error) come back as a typed <see cref="Result"/> failure
/// instead of throwing. Implemented in Infrastructure.
/// </summary>
public interface IFileContentReader
{
    Task<Result<string>> ReadTextAsync(string path, CancellationToken cancellationToken = default);
}
