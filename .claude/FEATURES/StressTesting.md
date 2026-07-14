# Stress Testing

## Overview

Stress Testing drives an Endpoint or Workflow under load and reports latency and throughput
statistics. It answers "how does this API behave under repetition and concurrency?" entirely
offline, against whatever target the user points it at. Runs are executed by a pluggable runner
behind a single contract, so load strategies can evolve without touching the core.

## Scope / Capabilities

Execution modes:

- **Sequential** — requests one after another.
- **Loop** — repeat a fixed number of iterations (optionally with a delay).
- **Concurrent** — N parallel virtual users / in-flight requests.

Collected metrics:

- **Average**, **Min**, **Max**, **Median** response time.
- **P95**, **P99** latency percentiles.
- **TPS** (transactions/sec), **RPS** (requests/sec).
- **Failure Rate** and **Timeout** count.

Runs honour cancellation and a configurable timeout per request; results feed the Dashboard and the
run/log tree.

## Domain & Contracts

Domain records (`ApiTestingStudio.Domain`):

- `StressTest` — target (endpoint or workflow id), `StressMode` (enum: `Sequential`, `Loop`,
  `Concurrent`), iteration/concurrency count, timeout, delay.
- `StressResult` — aggregate metrics (`Average`, `Min`, `Max`, `Median`, `P95`, `P99`, `Tps`,
  `Rps`, `FailureRate`, `TimeoutCount`) plus per-request samples.

Plugin contract (`ApiTestingStudio.Plugin.Abstractions`):

- `IStressRunner` —
  `Task<StressResult> RunAsync(StressTest test, IProgress<StressProgress> progress, CancellationToken cancellationToken)`.
- Ships as the `Runner.Stress` plugin; percentile/throughput math lives in the runner, and progress
  is streamed live via `IProgress<StressProgress>`.

## UI

- A **Stress Test** configuration panel: pick target, mode, iterations/concurrency, timeout.
- Live progress (completed / in-flight / failures) while running, then a metrics summary.
- Results visualised with **LiveCharts2** (latency distribution, throughput over time), hosted in
  an **AvalonDock** pane. MVVM (CommunityToolkit.Mvvm); Material Design.

## Sprint

- **Sprint 12** — `IStressRunner`, the `Runner.Stress` plugin, modes, and metric computation.

## Open Questions / Future

- Ramp-up / step-load profiles (gradually increasing concurrency).
- Duration-based runs (run for N seconds) in addition to iteration-based.
- Warm-up iterations excluded from statistics.
- Streaming histogram widget shared with the Dashboard.
- Assertion-driven pass/fail thresholds (e.g. fail if P99 > X ms).
