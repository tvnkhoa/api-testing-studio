using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Plugin.Abstractions.Runners;

/// <summary>
/// Parameters controlling a stress run. Describes only the load shape; the workload itself (which
/// request/endpoint/workflow to drive) is supplied to <see cref="IStressRunner.RunAsync"/> as a
/// delegate so the runner stays independent of the execution engine.
/// </summary>
public sealed record StressRunConfig
{
    /// <summary>Execution strategy.</summary>
    public StressMode Mode { get; init; } = StressMode.Sequential;

    /// <summary>
    /// Concurrent workers (VUs) for <see cref="StressMode.Concurrent"/>. Ignored (treated as 1) for
    /// sequential and loop modes.
    /// </summary>
    public int VirtualUsers { get; init; } = 1;

    /// <summary>
    /// Total number of workload invocations. For <see cref="StressMode.Sequential"/> this is 1.
    /// For <see cref="StressMode.Loop"/> it is the loop count. For <see cref="StressMode.Concurrent"/>
    /// it is the total request budget shared across all VUs, unless <see cref="Duration"/> is set.
    /// </summary>
    public int Iterations { get; init; } = 1;

    /// <summary>
    /// Optional wall-clock limit for <see cref="StressMode.Concurrent"/>. When set, the run stops once
    /// this elapses regardless of <see cref="Iterations"/>. Null means the iteration budget bounds the run.
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Warm-up invocations executed before measurement begins; their samples are excluded from the
    /// reported metrics. Zero disables warm-up.
    /// </summary>
    public int WarmupIterations { get; init; }
}
