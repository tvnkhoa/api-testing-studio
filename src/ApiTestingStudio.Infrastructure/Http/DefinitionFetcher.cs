using System.Net.Http;
using ApiTestingStudio.Application.Import;
using ApiTestingStudio.Shared.Results;
using Microsoft.Extensions.Logging;

namespace ApiTestingStudio.Infrastructure.Http;

/// <summary>
/// <see cref="IDefinitionFetcher"/> backed by a single long-lived <see cref="SocketsHttpHandler"/>.
/// Network access happens only here and only when the user explicitly starts a URL import. Given a
/// URL that already points at a definition it fetches directly; given a base URL it probes the common
/// definition paths in order and returns the first that responds with a definition-shaped body.
/// </summary>
public sealed class DefinitionFetcher : IDefinitionFetcher, IDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    // Probe order per .claude/FEATURES/Import.md.
    private static readonly string[] CandidatePaths =
    [
        "/openapi.json",
        "/openapi/v1.json",
        "/swagger.json",
        "/swagger/v1/swagger.json",
        "/scalar",
    ];

    private readonly HttpClient _client;
    private readonly ILogger<DefinitionFetcher> _logger;

    public DefinitionFetcher(ILogger<DefinitionFetcher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var handler = new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) };
        _client = new HttpClient(handler, disposeHandler: true) { Timeout = DefaultTimeout };
    }

    public async Task<Result<FetchedDefinition>> FetchAsync(string uri, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uri) || !Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
        {
            return Result.Failure<FetchedDefinition>(ImportErrors.FetchFailed($"'{uri}' is not a valid absolute URL."));
        }

        // 1. If the URL already points at a definition file, fetch it directly.
        if (HasDefinitionPath(parsed))
        {
            var direct = await TryFetchAsync(parsed.ToString(), cancellationToken).ConfigureAwait(false);
            if (direct is not null)
            {
                return Result.Success(direct);
            }
        }

        // 2. Otherwise (or if the direct fetch was not a definition) probe the well-known paths.
        var origin = $"{parsed.Scheme}://{parsed.Authority}";
        foreach (var candidate in CandidatePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var candidateUrl = origin + candidate;
            var fetched = await TryFetchAsync(candidateUrl, cancellationToken).ConfigureAwait(false);
            if (fetched is not null)
            {
                return Result.Success(fetched);
            }
        }

        return Result.Failure<FetchedDefinition>(
            ImportErrors.FetchFailed($"No API definition responded at '{origin}' (tried {CandidatePaths.Length} known paths)."));
    }

    private async Task<FetchedDefinition?> TryFetchAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return LooksLikeDefinition(body) ? new FetchedDefinition(body, url, Format: null) : null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogDebug(ex, "Definition probe failed for {Url}.", url);
            return null;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Per-request timeout — treat as "this candidate did not respond" and move on.
            _logger.LogDebug(ex, "Definition probe timed out for {Url}.", url);
            return null;
        }
    }

    private static bool HasDefinitionPath(Uri uri)
    {
        var path = uri.AbsolutePath;
        return path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
            || path.Contains("swagger", StringComparison.OrdinalIgnoreCase)
            || path.Contains("openapi", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeDefinition(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        var trimmed = body.TrimStart();

        // Reject HTML shells (e.g. the Scalar reference page) and accept JSON/YAML definitions.
        if (trimmed.StartsWith('<'))
        {
            return false;
        }

        return trimmed.StartsWith('{')
            || body.Contains("openapi", StringComparison.OrdinalIgnoreCase)
            || body.Contains("swagger", StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose() => _client.Dispose();
}
