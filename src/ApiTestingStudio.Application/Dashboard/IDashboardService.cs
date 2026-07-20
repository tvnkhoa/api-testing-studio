using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Dashboard;

/// <summary>
/// Aggregates the unified run store into a <see cref="DashboardSnapshot"/> for the Dashboard widgets
/// (Sprint 13). Reads only; requires an open workspace.
/// </summary>
public interface IDashboardService
{
    Task<Result<DashboardSnapshot>> GetSnapshotAsync(DashboardQuery query, CancellationToken cancellationToken = default);
}
