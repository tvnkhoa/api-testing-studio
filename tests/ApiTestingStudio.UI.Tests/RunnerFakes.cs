using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.ApiRunner;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.UI.Tests;

/// <summary>Canned <see cref="IRequestExecutionService"/> for runner view-model tests.</summary>
internal sealed class FakeRequestExecutionService : IRequestExecutionService
{
    public HttpExecutionResult ExecutionResult { get; set; } = new()
    {
        Response = new HttpResponseModel { StatusCode = 200, ReasonPhrase = "OK", Body = "{}", ContentLengthBytes = 2 },
        Timing = new RequestTiming { Total = TimeSpan.FromMilliseconds(10) },
    };

    public List<HttpRequestModel> Sent { get; } = [];

    public Task<Result<HttpExecutionResult>> SendAsync(Guid endpointId, HttpRequestModel request, CancellationToken cancellationToken = default)
    {
        Sent.Add(request);
        return Task.FromResult(Result.Success(ExecutionResult));
    }
}

/// <summary>In-memory <see cref="IRequestHistoryService"/>.</summary>
internal sealed class FakeRequestHistoryService : IRequestHistoryService
{
    public List<RequestHistoryEntry> Entries { get; } = [];

    public HttpRequestModel? ReplayRequest { get; set; }

    public Task<Result<IReadOnlyList<RequestHistoryEntry>>> GetHistoryAsync(Guid endpointId, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success<IReadOnlyList<RequestHistoryEntry>>(Entries));

    public Task<Result<HttpRequestModel>> GetRequestForReplayAsync(Guid historyEntryId, CancellationToken cancellationToken = default)
        => Task.FromResult(ReplayRequest is null
            ? Result.Failure<HttpRequestModel>(RequestExecutionErrors.HistoryEntryNotFound(historyEntryId))
            : Result.Success(ReplayRequest));

    public Task<Result> ClearHistoryAsync(Guid endpointId, CancellationToken cancellationToken = default)
    {
        Entries.Clear();
        return Task.FromResult(Result.Success());
    }
}

/// <summary>In-memory <see cref="IEndpointRepository"/>.</summary>
internal sealed class FakeEndpointRepository : IEndpointRepository
{
    public List<Endpoint> Endpoints { get; } = [];

    public Task<IReadOnlyList<Endpoint>> GetByServiceAsync(Guid serviceId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Endpoint>>(Endpoints.Where(e => e.ServiceId == serviceId).ToList());

    public Task<Endpoint?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Endpoints.FirstOrDefault(e => e.Id == id));

    public Task AddAsync(Endpoint endpoint, CancellationToken cancellationToken = default)
    {
        Endpoints.Add(endpoint);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Endpoint endpoint, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

/// <summary>In-memory <see cref="IServiceRepository"/>.</summary>
internal sealed class FakeServiceRepository : IServiceRepository
{
    public List<Service> Services { get; } = [];

    public Task<IReadOnlyList<Service>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Service>>(Services.Where(s => s.WorkspaceId == workspaceId).ToList());

    public Task<Service?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Services.FirstOrDefault(s => s.Id == id));

    public Task AddAsync(Service service, CancellationToken cancellationToken = default)
    {
        Services.Add(service);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Service service, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DeleteCascadeAsync(Guid serviceId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
