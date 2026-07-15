using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.ApiRunner;

/// <summary>
/// Default <see cref="IRequestHistoryService"/> over <see cref="IRequestHistoryRepository"/>.
/// Workspace scope comes from <see cref="IWorkspaceSession"/>.
/// </summary>
public sealed class RequestHistoryService : IRequestHistoryService
{
    private readonly IRequestHistoryRepository _history;
    private readonly IWorkspaceSession _session;

    public RequestHistoryService(IRequestHistoryRepository history, IWorkspaceSession session)
    {
        _history = history;
        _session = session;
    }

    public async Task<Result<IReadOnlyList<RequestHistoryEntry>>> GetHistoryAsync(Guid endpointId, CancellationToken cancellationToken = default)
    {
        if (!_session.IsOpen)
        {
            return Result.Failure<IReadOnlyList<RequestHistoryEntry>>(RequestExecutionErrors.NoWorkspaceOpen);
        }

        var entries = await _history.GetByEndpointAsync(endpointId, cancellationToken).ConfigureAwait(false);
        return Result.Success(entries);
    }

    public async Task<Result<HttpRequestModel>> GetRequestForReplayAsync(Guid historyEntryId, CancellationToken cancellationToken = default)
    {
        if (!_session.IsOpen)
        {
            return Result.Failure<HttpRequestModel>(RequestExecutionErrors.NoWorkspaceOpen);
        }

        var entry = await _history.GetAsync(historyEntryId, cancellationToken).ConfigureAwait(false);
        if (entry is null)
        {
            return Result.Failure<HttpRequestModel>(RequestExecutionErrors.HistoryEntryNotFound(historyEntryId));
        }

        var request = RequestSnapshotSerializer.DeserializeRequest(entry.RequestSnapshot);
        if (request is null)
        {
            return Result.Failure<HttpRequestModel>(RequestExecutionErrors.HistoryEntryNotFound(historyEntryId));
        }

        return Result.Success(request);
    }

    public async Task<Result> ClearHistoryAsync(Guid endpointId, CancellationToken cancellationToken = default)
    {
        if (!_session.IsOpen)
        {
            return Result.Failure(RequestExecutionErrors.NoWorkspaceOpen);
        }

        await _history.DeleteByEndpointAsync(endpointId, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
