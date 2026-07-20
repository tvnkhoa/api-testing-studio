using ApiTestingStudio.Plugin.Abstractions.Runners;

namespace ApiTestingStudio.Runner.Stress;

/// <summary>
/// Thread-safe <see cref="IMetricsAggregator"/>. Min/avg/max are tracked exactly; percentiles come
/// from a bounded <see cref="LatencyHistogram"/> so memory stays fixed under high volume. A single
/// lock guards accumulation — adequate for a local desktop load generator where correctness and
/// bounded memory matter more than shaving lock overhead. Reusable by the Dashboard (Sprint 13) via
/// the <see cref="IMetricsAggregator"/> contract.
/// </summary>
internal sealed class MetricsAggregator : IMetricsAggregator
{
    private readonly object _gate = new();
    private readonly LatencyHistogram _histogram = new();
    private readonly ThroughputMeter _throughput = new();

    private long _failed;
    private double _sumMs;
    private double _minMs = double.MaxValue;
    private double _maxMs;

    public void Record(StressSample sample)
    {
        lock (_gate)
        {
            _throughput.Increment();
            if (!sample.Success)
            {
                _failed++;
            }

            var ms = sample.ElapsedMs;
            _sumMs += ms;
            if (ms < _minMs)
            {
                _minMs = ms;
            }

            if (ms > _maxMs)
            {
                _maxMs = ms;
            }

            _histogram.Record(ms);
        }
    }

    public StressMetricsSnapshot Snapshot(TimeSpan elapsed)
    {
        lock (_gate)
        {
            var completed = _throughput.Completed;
            if (completed == 0)
            {
                return StressMetricsSnapshot.Empty with { Elapsed = elapsed };
            }

            return new StressMetricsSnapshot
            {
                Elapsed = elapsed,
                Completed = completed,
                Failed = _failed,
                RequestsPerSecond = _throughput.RequestsPerSecond(elapsed),
                MinMs = _minMs,
                AverageMs = _sumMs / completed,
                MaxMs = _maxMs,
                P50Ms = _histogram.Percentile(50),
                P95Ms = _histogram.Percentile(95),
                P99Ms = _histogram.Percentile(99),
                ErrorRate = (double)_failed / completed,
            };
        }
    }
}
