using ApiTestingStudio.Application.Abstractions;
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

    public RequestExecutionService(
        IRequestExecutor executor,
        IRequestHistoryRepository history,
        IWorkspaceSession session,
        IClock clock)
    {
        _executor = executor;
        _history = history;
        _session = session;
        _clock = clock;
    }

    public async Task<Result<HttpExecutionResult>> SendAsync(Guid endpointId, HttpRequestModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_session.IsOpen)
        {
            return Result.Failure<HttpExecutionResult>(RequestExecutionErrors.NoWorkspaceOpen);
        }

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
        return result;
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
