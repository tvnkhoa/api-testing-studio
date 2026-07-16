using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.ApiRunner;

/// <summary>
/// Use-case service for the API Runner: validates and sends a request for an endpoint, then records
/// it in history. Orchestrates the <see cref="Abstractions.IRequestExecutor"/> and
/// <see cref="Abstractions.IRequestHistoryRepository"/> ports; holds no persistence or HTTP types.
/// </summary>
public interface IRequestExecutionService
{
    /// <summary>
    /// Sends <paramref name="request"/> for the given endpoint and, on a completed HTTP response,
    /// appends a history entry. Before sending, <c>{{variables}}</c> in the request are resolved
    /// against the workspace + active-environment scopes, and when <paramref name="profileId"/> is
    /// supplied that profile's authorization is applied. Transport failures (invalid URL, timeout,
    /// cancellation, network) are returned as typed failures and are not recorded.
    /// </summary>
    Task<Result<HttpExecutionResult>> SendAsync(Guid endpointId, HttpRequestModel request, Guid? profileId = null, CancellationToken cancellationToken = default);
}
