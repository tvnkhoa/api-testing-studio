# Sprint 12 — Stress Runner

## Goal
Implement the `Runner.Stress` plugin (`IStressRunner`) to drive load with sequential, looped, and concurrent execution modes and produce metrics: throughput (TPS/RPS), latency percentiles (P95/P99), and error rates.

## Scope
- `IStressRunner` in plugins/Runner.Stress.
- Execution modes: sequential, loop (N iterations), concurrent (fixed VUs / ramp — ramp uncertain).
- Metrics collection: request count, TPS/RPS, min/avg/max latency, P50/P95/P99, error rate.
- Live metrics stream during a run + final summary.
- Reuses S06 `IRequestExecutor`; can target a request, endpoint, or workflow.

## Requirements
- Bounded concurrency with configurable virtual users and duration/iteration limits.
- Low-overhead metrics aggregation (histogram/streaming percentiles).
- Cancellation stops the run cleanly and finalizes partial metrics.
- Results persist for later review in the Dashboard (S13).

## Architecture Impact
- Introduces a load-generation service and a metrics aggregation component reusable by Dashboard.
- Streaming percentile estimator (e.g. HdrHistogram-style) to avoid storing every sample.

## Projects (which solution projects change)
- Plugin.Abstractions — `IStressRunner`, stress config/result contracts.
- plugins/Runner.Stress — runner implementation.
- Domain — StressRun, StressMetrics models.
- Application — stress orchestration, metrics aggregation.
- Infrastructure — run/metrics persistence + migration.
- UI — stress config + live metrics panel.
- Tests: PluginHost.Tests, Application.Tests.

## Classes
- `StressRunner` (`IStressRunner`), `SequentialStrategy`, `LoopStrategy`, `ConcurrentStrategy`.
- `MetricsAggregator`, `LatencyHistogram`, `ThroughputMeter`.
- `StressRunConfig`, `StressRunResult`, `StressMetricsSnapshot`.
- `StressRunnerViewModel`, `LiveMetricsViewModel`.

## Interfaces
- `IStressRunner` (configure + run + stream metrics).
- `IMetricsAggregator`, `IStressRunStore`.

## Database Changes
- New tables: `StressRuns`, `StressMetrics` (summary + optional time-series).
- Migration: `AddStressRuns`.

## Plugin Changes
- Implement Runner.Stress against `IStressRunner`; declare `IRunnerPlugin` capability.

## UI Changes
- Stress configuration panel (mode, VUs, duration/iterations, target).
- Live metrics view (counters + latency/throughput readouts; charts land in S13).

## Acceptance Criteria
- Run sequential, loop, and concurrent stress against a target.
- TPS/RPS, P95, and P99 are computed and displayed.
- Error rate reflects failed responses.
- Cancellation finalizes and persists partial results.
- Runner loads as a plugin and is selectable in the UI.

## Out of Scope
- Distributed/multi-agent load generation.
- Rich charting (Sprint 13 dashboard).
- SLA thresholds/alerting (possible future).

## Risks
- Percentile accuracy vs memory tradeoffs under high volume.
- Thread-pool starvation / measurement skew from local overhead.
- HttpClient connection pooling limits affecting concurrency realism.

## Future Improvements
- Ramp-up/ramp-down and arrival-rate (open model) load.
- Pass/fail SLA gates.
- Distributed load agents.

## Checklist
- [ ] `IStressRunner` contract + config/result models.
- [ ] Sequential/loop/concurrent strategies.
- [ ] Metrics aggregator + streaming percentiles.
- [ ] Persistence + migration.
- [ ] Stress config + live metrics UI + plugin wiring.
