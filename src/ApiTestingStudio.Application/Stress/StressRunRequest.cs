using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Plugin.Abstractions.Runners;

namespace ApiTestingStudio.Application.Stress;

/// <summary>
/// A request to run a stress test: the load shape (<see cref="Config"/>) plus what to drive. The
/// orchestrator resolves the target into a workload delegate — an ad-hoc <see cref="Request"/>, a saved
/// <see cref="EndpointId"/>, or a saved <see cref="WorkflowId"/> — so the runner stays decoupled from
/// the execution engine.
/// </summary>
public sealed record StressRunRequest
{
    /// <summary>Load shape (mode, virtual users, iterations/duration, warm-up).</summary>
    public required StressRunConfig Config { get; init; }

    /// <summary>What to drive.</summary>
    public StressTargetKind TargetKind { get; init; }

    /// <summary>Saved endpoint to stress when <see cref="TargetKind"/> is <see cref="StressTargetKind.Endpoint"/>.</summary>
    public Guid? EndpointId { get; init; }

    /// <summary>Saved workflow to stress when <see cref="TargetKind"/> is <see cref="StressTargetKind.Workflow"/>.</summary>
    public Guid? WorkflowId { get; init; }

    /// <summary>
    /// Ad-hoc request to stress when <see cref="TargetKind"/> is <see cref="StressTargetKind.Request"/>.
    /// </summary>
    public HttpRequestModel? Request { get; init; }

    /// <summary>Optional display label; when null the orchestrator derives one from the target.</summary>
    public string? TargetName { get; init; }
}
