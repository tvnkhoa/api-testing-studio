using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Plugin.Abstractions.Runners;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Stress;

/// <summary>
/// Orchestrates a stress run: resolves the target into a workload, drives it through the
/// <see cref="IStressRunner"/> plugin, streams live metrics, and persists the result. This is the seam
/// where the stress runner (pure load generation) is wired to the request executor (Sprint 06) and
/// workflow engine (Sprint 08). Pure orchestration; holds no HTTP or persistence types.
/// </summary>
public interface IStressOrchestrator
{
    /// <summary>
    /// Runs a stress test and persists it. Returns a failure only when the run cannot start (no
    /// workspace open, no runner plugin, missing/invalid target); a run that completes — even one that
    /// was cancelled — succeeds and yields the persisted <see cref="StressRun"/>. Live metrics are
    /// pushed to <paramref name="progress"/> during the run.
    /// </summary>
    Task<Result<StressRun>> RunAsync(
        StressRunRequest request,
        IProgress<StressMetricsSnapshot>? progress = null,
        CancellationToken cancellationToken = default);
}
