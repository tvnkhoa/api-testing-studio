using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Import;

/// <summary>
/// Fetches an API definition from a URL. This is the single, auditable place where import performs
/// network access — it happens only when the user explicitly triggers a URL-based import
/// (offline-first). Given a base URL it probes the common definition paths in order and returns the
/// first that responds. Implemented in Infrastructure.
/// </summary>
public interface IDefinitionFetcher
{
    Task<Result<FetchedDefinition>> FetchAsync(string uri, CancellationToken cancellationToken = default);
}
