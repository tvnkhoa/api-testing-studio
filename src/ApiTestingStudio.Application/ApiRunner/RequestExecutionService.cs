using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Profiles;
using ApiTestingStudio.Application.Runs;
using ApiTestingStudio.Application.Variables;
using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.ApiRunner;

/// <summary>
/// Default <see cref="IRequestExecutionService"/>. Guards workspace scope via
/// <see cref="IWorkspaceSession"/>, validates the URL, delegates the send to
/// <see cref="IRequestExecutor"/>, and records a <see cref="RequestHistoryEntry"/> on success.
/// </summary>
public sealed class RequestExecutionService : IRequestExecutionService
{
    private readonly IRequestExecutor _executor;
    private readonly IRequestHistoryRepository _history;
    private readonly IWorkspaceSession _session;
    private readonly IClock _clock;
    private readonly IVariableScopeSeeder _scopeSeeder;
    private readonly IVariableResolver _resolver;
    private readonly IProfileRepository _profiles;
    private readonly IAuthApplicator _authApplicator;
    private readonly IRunRecorder _runRecorder;

    public RequestExecutionService(
        IRequestExecutor executor,
        IRequestHistoryRepository history,
        IWorkspaceSession session,
        IClock clock,
        IVariableScopeSeeder scopeSeeder,
        IVariableResolver resolver,
        IProfileRepository profiles,
        IAuthApplicator authApplicator,
        IRunRecorder runRecorder)
    {
        _executor = executor;
        _history = history;
        _session = session;
        _clock = clock;
        _scopeSeeder = scopeSeeder;
        _resolver = resolver;
        _profiles = profiles;
        _authApplicator = authApplicator;
        _runRecorder = runRecorder;
    }

    public async Task<Result<HttpExecutionResult>> SendAsync(Guid endpointId, HttpRequestModel request, Guid? profileId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_session.IsOpen)
        {
            return Result.Failure<HttpExecutionResult>(RequestExecutionErrors.NoWorkspaceOpen);
        }

        var unresolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        request = await ResolveAndAuthorizeAsync(request, profileId, unresolved, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return Result.Failure<HttpExecutionResult>(RequestExecutionErrors.UrlRequired);
        }

        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Result.Failure<HttpExecutionResult>(RequestExecutionErrors.InvalidUrl(request.Url));
        }

        var result = await _executor.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return result;
        }

        await RecordHistoryAsync(endpointId, request, result.Value, cancellationToken).ConfigureAwait(false);
        await _runRecorder.RecordRequestAsync(endpointId, request, result.Value, cancellationToken).ConfigureAwait(false);

        // Carry any unresolved {{tokens}} back to the caller so the Runner can warn instead of the
        // substitution silently producing empty values.
        return unresolved.Count > 0
            ? Result.Success(result.Value with { Warnings = [.. unresolved] })
            : result;
    }

    /// <summary>
    /// Resolves <c>{{variables}}</c> in the request against the workspace + active-environment scopes
    /// and, when a profile is selected, applies its authorization header.
    /// </summary>
    private async Task<HttpRequestModel> ResolveAndAuthorizeAsync(HttpRequestModel request, Guid? profileId, ICollection<string> unresolved, CancellationToken cancellationToken)
    {
        var context = await _scopeSeeder.BuildContextAsync(cancellationToken).ConfigureAwait(false);

        var resolved = request with
        {
            Url = _resolver.Resolve(request.Url, context, unresolved),
            QueryParams = request.QueryParams
                .Select(p => p with { Value = _resolver.Resolve(p.Value, context, unresolved) })
                .ToList(),
            Headers = request.Headers
                .Select(h => h with { Value = _resolver.Resolve(h.Value, context, unresolved) })
                .ToList(),
            Body = string.IsNullOrEmpty(request.Body) ? request.Body : _resolver.Resolve(request.Body, context, unresolved),
        };

        if (profileId is { } id)
        {
            var profile = await _profiles.GetAsync(id, cancellationToken).ConfigureAwait(false);
            resolved = _authApplicator.Apply(resolved, profile);
        }

        return resolved;
    }

    private async Task RecordHistoryAsync(
        Guid endpointId,
        HttpRequestModel request,
        HttpExecutionResult execution,
        CancellationToken cancellationToken)
    {
        var timing = execution.Timing;
        var entry = new RequestHistoryEntry
        {
            EndpointId = endpointId,
            Method = request.Method,
            Url = request.Url,
            StatusCode = execution.Response.StatusCode,
            TotalMs = (long)timing.Total.TotalMilliseconds,
            DnsMs = ToMs(timing.Dns),
            ConnectMs = ToMs(timing.Connect),
            TimeToFirstByteMs = ToMs(timing.TimeToFirstByte),
            RequestSnapshot = RequestSnapshotSerializer.Serialize(request),
            ResponseSnapshot = RequestSnapshotSerializer.Serialize(execution.Response),
            TimestampUtc = _clock.UtcNow,
        };

        await _history.AddAsync(entry, cancellationToken).ConfigureAwait(false);
    }

    private static long? ToMs(TimeSpan? span) =>
        span is { } value ? (long)value.TotalMilliseconds : null;
}
