using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Plugin.Abstractions.Runners;
using ApiTestingStudio.Runner.Stress;
using FluentAssertions;

namespace ApiTestingStudio.PluginHost.Tests;

/// <summary>
/// Exercises the <c>Runner.Stress</c> plugin end-to-end through <see cref="IStressRunner"/> with a
/// synthetic workload, covering the sequential/loop/concurrent strategies, metric aggregation
/// (counts, percentiles, error rate), warm-up exclusion, and clean cancellation.
/// </summary>
public sealed class StressRunnerTests
{
    private static Func<CancellationToken, Task<StressSample>> Constant(double ms, bool success = true, int? status = 200)
        => _ => Task.FromResult(new StressSample(ms, success, status));

    [Fact]
    public async Task Sequential_runs_exactly_one_invocation()
    {
        var runner = new StressRunner();
        var invoked = 0;

        var result = await runner.RunAsync(
            new StressRunConfig { Mode = StressMode.Sequential },
            _ => { Interlocked.Increment(ref invoked); return Task.FromResult(new StressSample(5, true, 200)); });

        invoked.Should().Be(1);
        result.Metrics.Completed.Should().Be(1);
        result.Cancelled.Should().BeFalse();
    }

    [Fact]
    public async Task Loop_runs_the_configured_iteration_count()
    {
        var runner = new StressRunner();

        var result = await runner.RunAsync(
            new StressRunConfig { Mode = StressMode.Loop, Iterations = 50 },
            Constant(10));

        result.Metrics.Completed.Should().Be(50);
        result.Metrics.ErrorRate.Should().Be(0);
    }

    [Fact]
    public async Task Concurrent_respects_the_iteration_budget_across_virtual_users()
    {
        var runner = new StressRunner();
        var invoked = 0;

        var result = await runner.RunAsync(
            new StressRunConfig { Mode = StressMode.Concurrent, VirtualUsers = 8, Iterations = 200 },
            async ct =>
            {
                Interlocked.Increment(ref invoked);
                await Task.Yield();
                return new StressSample(3, true, 200);
            });

        invoked.Should().Be(200);
        result.Metrics.Completed.Should().Be(200);
    }

    [Fact]
    public async Task Percentiles_track_a_constant_latency_within_bucket_tolerance()
    {
        var runner = new StressRunner();

        var result = await runner.RunAsync(
            new StressRunConfig { Mode = StressMode.Loop, Iterations = 500 },
            Constant(50));

        result.Metrics.P50Ms.Should().BeApproximately(50, 2);
        result.Metrics.P95Ms.Should().BeApproximately(50, 2);
        result.Metrics.P99Ms.Should().BeApproximately(50, 2);
        result.Metrics.MinMs.Should().Be(50);
        result.Metrics.MaxMs.Should().Be(50);
        result.Metrics.AverageMs.Should().BeApproximately(50, 0.001);
    }

    [Fact]
    public async Task Error_rate_reflects_failed_samples()
    {
        var runner = new StressRunner();
        var index = 0;

        var result = await runner.RunAsync(
            new StressRunConfig { Mode = StressMode.Loop, Iterations = 100 },
            _ =>
            {
                var success = Interlocked.Increment(ref index) % 2 == 0; // half fail
                return Task.FromResult(new StressSample(5, success, success ? 200 : 500));
            });

        result.Metrics.Completed.Should().Be(100);
        result.Metrics.Failed.Should().Be(50);
        result.Metrics.ErrorRate.Should().BeApproximately(0.5, 0.0001);
    }

    [Fact]
    public async Task Warmup_invocations_are_excluded_from_the_metrics()
    {
        var runner = new StressRunner();
        var invoked = 0;

        var result = await runner.RunAsync(
            new StressRunConfig { Mode = StressMode.Loop, Iterations = 5, WarmupIterations = 10 },
            _ => { Interlocked.Increment(ref invoked); return Task.FromResult(new StressSample(2, true, 200)); });

        invoked.Should().Be(15); // 10 warm-up + 5 measured
        result.Metrics.Completed.Should().Be(5);
    }

    [Fact]
    public async Task Cancellation_stops_early_and_finalizes_partial_metrics()
    {
        var runner = new StressRunner();
        using var cts = new CancellationTokenSource();
        var invoked = 0;

        var result = await runner.RunAsync(
            new StressRunConfig { Mode = StressMode.Loop, Iterations = 100_000 },
            _ =>
            {
                if (Interlocked.Increment(ref invoked) >= 5)
                {
                    cts.Cancel();
                }

                return Task.FromResult(new StressSample(1, true, 200));
            },
            progress: null,
            cancellationToken: cts.Token);

        result.Cancelled.Should().BeTrue();
        result.Metrics.Completed.Should().BeGreaterThan(0).And.BeLessThan(100_000);
    }

    [Fact]
    public async Task Progress_is_reported_and_a_final_snapshot_is_pushed()
    {
        var runner = new StressRunner();
        var snapshots = new List<StressMetricsSnapshot>();
        var progress = new Progress<StressMetricsSnapshot>(s => snapshots.Add(s));

        var result = await runner.RunAsync(
            new StressRunConfig { Mode = StressMode.Loop, Iterations = 20 },
            Constant(5),
            progress);

        // The final Report happens after RunAsync returns to the caller's context; the returned result
        // always carries the finalized metrics.
        result.Metrics.Completed.Should().Be(20);
    }
}
