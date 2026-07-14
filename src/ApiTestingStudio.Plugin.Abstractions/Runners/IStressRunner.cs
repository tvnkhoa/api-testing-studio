namespace ApiTestingStudio.Plugin.Abstractions.Runners;

/// <summary>How a stress run issues requests.</summary>
public enum StressMode
{
    Sequential,
    Loop,
    Concurrent,
}

/// <summary>Parameters controlling a stress run.</summary>
public sealed record StressPlan(StressMode Mode, int Iterations, int Concurrency);

/// <summary>Aggregate metrics collected from a stress run.</summary>
public sealed record StressMetrics(
    double AverageMs,
    double MinMs,
    double MaxMs,
    double MedianMs,
    double P95Ms,
    double P99Ms,
    double RequestsPerSecond,
    double FailureRate);

/// <summary>
/// Executes a stress plan and collects performance metrics. Implemented by <c>Runner.Stress</c>.
/// </summary>
public interface IStressRunner
{
    /// <summary>Runs the given plan and returns aggregate metrics.</summary>
    Task<StressMetrics> RunAsync(StressPlan plan, CancellationToken cancellationToken = default);
}
