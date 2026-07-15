using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Low-level HTTP execution port: sends a fully-assembled <see cref="HttpRequestModel"/> and returns
/// the response plus timing. The concrete <c>HttpClient</c>-backed adapter lives in Infrastructure;
/// keeping this a port lets Workflow (Sprint 08) and Stress (Sprint 12) reuse the same execution
/// engine and lets tests substitute a fake. Transport failures (network, timeout, cancellation) are
/// returned as typed <see cref="Result"/> failures rather than thrown.
/// </summary>
public interface IRequestExecutor
{
    Task<Result<HttpExecutionResult>> ExecuteAsync(HttpRequestModel request, CancellationToken cancellationToken = default);
}
