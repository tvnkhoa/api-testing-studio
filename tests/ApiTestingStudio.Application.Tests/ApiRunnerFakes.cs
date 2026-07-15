using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Tests;

/// <summary>Configurable in-memory <see cref="IRequestExecutor"/> — no real HTTP.</summary>
internal sealed class FakeRequestExecutor : IRequestExecutor
{
    public FakeRequestExecutor()
    {
        var response = new HttpResponseModel
        {
            StatusCode = 200,
            ReasonPhrase = "OK",
            Body = "{\"ok\":true}",
            ContentLengthBytes = 11,
        };
        ResultToReturn = Result.Success(new HttpExecutionResult
        {
            Response = response,
            Timing = new RequestTiming { Total = TimeSpan.FromMilliseconds(42) },
        });
    }

    public Result<HttpExecutionResult> ResultToReturn { get; set; }

    public HttpRequestModel? LastRequest { get; private set; }

    public int CallCount { get; private set; }

    public Task<Result<HttpExecutionResult>> ExecuteAsync(HttpRequestModel request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        CallCount++;
        return Task.FromResult(ResultToReturn);
    }
}

/// <summary>Shared in-memory <see cref="IRequestHistoryRepository"/>.</summary>
internal sealed class InMemoryRequestHistoryRepository : IRequestHistoryRepository
{
    public List<RequestHistoryEntry> Entries { get; } = [];

    public Task<IReadOnlyList<RequestHistoryEntry>> GetByEndpointAsync(Guid endpointId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<RequestHistoryEntry>>(Entries
            .Where(e => e.EndpointId == endpointId)
            .OrderByDescending(e => e.TimestampUtc)
            .ToList());

    public Task<RequestHistoryEntry?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Entries.FirstOrDefault(e => e.Id == id));

    public Task AddAsync(RequestHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task DeleteByEndpointAsync(Guid endpointId, CancellationToken cancellationToken = default)
    {
        Entries.RemoveAll(e => e.EndpointId == endpointId);
        return Task.CompletedTask;
    }
}
