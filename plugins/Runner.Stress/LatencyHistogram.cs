namespace ApiTestingStudio.Runner.Stress;

/// <summary>
/// A bounded, log-linear latency histogram (HdrHistogram-style) that estimates percentiles without
/// storing every sample. Buckets grow geometrically (~2% relative width) from
/// <see cref="MinValueMs"/> to <see cref="MaxValueMs"/>, so memory is fixed regardless of request
/// volume and percentile error stays within one bucket width. Not thread-safe on its own;
/// <see cref="MetricsAggregator"/> serializes access.
/// </summary>
internal sealed class LatencyHistogram
{
    private const double MinValueMs = 0.1;
    private const double MaxValueMs = 600_000; // 10 minutes — clamps pathological outliers.
    private const double Growth = 1.02;        // ~2% relative bucket width.

    private static readonly double LogGrowth = Math.Log(Growth);

    private readonly long[] _buckets;
    private long _count;

    public LatencyHistogram()
    {
        var span = (int)Math.Ceiling(Math.Log(MaxValueMs / MinValueMs) / LogGrowth);
        // +1 for the sub-min bucket at index 0, +1 for the overflow bucket at the end.
        _buckets = new long[span + 2];
    }

    public long Count => _count;

    public void Record(double valueMs)
    {
        _count++;
        _buckets[IndexOf(valueMs)]++;
    }

    /// <summary>Estimates the <paramref name="percentile"/> (0..100) latency in milliseconds.</summary>
    public double Percentile(double percentile)
    {
        if (_count == 0)
        {
            return 0;
        }

        var rank = (long)Math.Ceiling(percentile / 100.0 * _count);
        if (rank < 1)
        {
            rank = 1;
        }

        long cumulative = 0;
        for (var i = 0; i < _buckets.Length; i++)
        {
            cumulative += _buckets[i];
            if (cumulative >= rank)
            {
                return Representative(i);
            }
        }

        return Representative(_buckets.Length - 1);
    }

    private int IndexOf(double valueMs)
    {
        if (valueMs <= MinValueMs)
        {
            return 0;
        }

        if (valueMs >= MaxValueMs)
        {
            return _buckets.Length - 1;
        }

        // +1 so index 0 stays reserved for the sub-min bucket.
        return (int)(Math.Log(valueMs / MinValueMs) / LogGrowth) + 1;
    }

    private static double Representative(int index)
    {
        if (index == 0)
        {
            return MinValueMs;
        }

        // Geometric midpoint of the bucket's [lower, upper) bounds.
        var lower = MinValueMs * Math.Pow(Growth, index - 1);
        var upper = MinValueMs * Math.Pow(Growth, index);
        return (lower + upper) / 2.0;
    }
}
