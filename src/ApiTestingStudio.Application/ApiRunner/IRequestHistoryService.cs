using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.ApiRunner;

/// <summary>
/// Reads and replays per-endpoint request history. Replay reconstructs the original
/// <see cref="HttpRequestModel"/> from its stored snapshot so it can be re-sent through
/// <see cref="IRequestExecutionService"/>.
/// </summary>
public interface IRequestHistoryService
{
    /// <summary>Returns an endpoint's history, most-recent first.</summary>
    Task<Result<IReadOnlyList<RequestHistoryEntry>>> GetHistoryAsync(Guid endpointId, CancellationToken cancellationToken = default);

    /// <summary>Rebuilds the request from a history entry for replay.</summary>
    Task<Result<HttpRequestModel>> GetRequestForReplayAsync(Guid historyEntryId, CancellationToken cancellationToken = default);

    /// <summary>Clears all history for an endpoint.</summary>
    Task<Result> ClearHistoryAsync(Guid endpointId, CancellationToken cancellationToken = default);
}
