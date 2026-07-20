namespace ApiTestingStudio.Plugin.Abstractions.Runners;

/// <summary>
/// The outcome of one workload invocation during a stress run: how long it took, whether it
/// succeeded, and (for HTTP targets) the response status code. Kept a lightweight value type so
/// high-volume aggregation avoids per-sample heap allocations.
/// </summary>
public readonly record struct StressSample(double ElapsedMs, bool Success, int? StatusCode);
