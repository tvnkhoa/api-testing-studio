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
- [x] `IStressRunner` contract + config/result models.
- [x] Sequential/loop/concurrent strategies.
- [x] Metrics aggregator + streaming percentiles.
- [x] Persistence + migration.
- [x] Stress config + live metrics UI + plugin wiring.

## Implementation notes (delivered)
- **Contracts** (`Plugin.Abstractions/Runners`): `StressRunConfig`, `StressSample`,
  `StressMetricsSnapshot`, `StressRunResult`, `IMetricsAggregator`; `IStressRunner.RunAsync` takes a
  `Func<CancellationToken, Task<StressSample>>` **workload delegate** + `IProgress<StressMetricsSnapshot>`
  so the runner is decoupled from the execution engine. `StressMode`/`StressTargetKind` live in
  `Domain.Enums` (shared by the persisted entity and the contract).
- **Plugin** (`plugins/Runner.Stress`): `StressRunner` + `Sequential/Loop/ConcurrentStrategy`
  (bounded VUs via a shared iteration budget / `CancelAfter` duration), `MetricsAggregator`
  (thread-safe), `LatencyHistogram` (bounded log-linear, ~2% error, no per-sample storage — no new
  NuGet), `ThroughputMeter`. Live snapshots pumped every 250 ms via `PeriodicTimer`.
- **Orchestration** (`Application/Stress`): `IStressOrchestrator`/`StressOrchestrator` resolves an
  endpoint/workflow/ad-hoc-request target into the workload (raw `IRequestExecutor` — not the
  history-recording service — or `IWorkflowEngine`), stamps timestamps from `IClock`, persists via
  `IStressRunStore`. `StressRunRequest`/`StressErrors`.
- **Domain**: `StressRun` (+ headline metrics) and `StressMetrics` records.
- **Infrastructure**: `StressRuns`/`StressMetrics` tables + `AddStressRuns` migration; schema
  version bumped **7 → 8**; `StressRunRepository : IStressRunStore`.
- **UI** (`UI/ViewModels/Stress`): `StressRunnerViewModel` (config + target picker, Run/Stop via
  `IncludeCancelCommand`) + `LiveMetricsViewModel`, `StressRunnerView` + DataTemplate; opened from
  **View → Stress Runner**; the loaded runner plugin is reported via `IPluginRegistry` and Run is
  disabled when none is present.
- **Tests**: `PluginHost.Tests/StressRunnerTests` (strategies, percentiles, error rate, warm-up,
  cancellation) and `Application.Tests/StressOrchestratorTests` (target resolution, persistence,
  runner-unavailable / no-workspace). Full suite green (277 tests), build 0 warnings.
