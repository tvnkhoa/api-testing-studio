# Sprint 13 — Dashboard & Logging

## Goal
Provide a live dashboard with LiveCharts2 visualizations, an execution timeline, run replay, monitoring, and structured log viewing — the observability surface across runs, workflows, and stress tests.

## Scope
- Live dashboard aggregating request/workflow/stress metrics (LiveCharts2).
- Execution timeline of node/request events with drill-down.
- Run replay: re-view a completed run's steps and results.
- Monitoring widgets (success rate, latency trends, throughput).
- Log viewer over Serilog output (structured, filterable).

## Requirements
- Charts update live during active runs without blocking the UI thread.
- Timeline reconstructs from persisted run results (S08/S11/S12).
- Log viewer reads Serilog sink (file/in-memory) with level/source filters.
- Dashboard is composable from reusable chart/widget controls.

## Architecture Impact
- Introduces a metrics/event streaming pipeline feeding the UI (observable streams).
- Standardizes a Serilog sink the log viewer consumes.
- Reuses run/metrics stores from S08, S11, S12.

## Projects (which solution projects change)
- UI — dashboard panel, chart widgets, timeline, log viewer.
- Application — metrics/event aggregation, replay service.
- Infrastructure — Serilog sink for the viewer, run-result reads.
- Tests: Application.Tests.

## Classes
- `DashboardViewModel`, `TimelineViewModel`, `LogViewerViewModel`, `ReplayViewModel`.
- `MetricsStreamAggregator`, `RunReplayService`, `LogEventSink` (Serilog).
- Chart widgets: `LatencyChart`, `ThroughputChart`, `SuccessRateGauge` (LiveCharts2).

## Interfaces
- `IMetricsStream`, `IRunReplayService`, `ILogEventSource`.
- `IDashboardWidget` (uncertain — widget composition abstraction).

## Database Changes
- None new expected. (Reads existing run/metrics tables; log store may be file-based, not DB.)
- Uncertain: a `LogEvents` table if in-DB log persistence is desired.

## Plugin Changes
- None. (Dashboard consumes results produced by runner/assertion plugins.)

## UI Changes
- New Dashboard panel with LiveCharts2 charts and monitoring widgets.
- Timeline panel with drill-down.
- Log viewer panel with filtering/search.
- Replay controls.

## Acceptance Criteria
- Dashboard shows live charts during a request/workflow/stress run.
- Timeline reconstructs a completed run and supports drill-down.
- Replay walks through a prior run's steps.
- Log viewer displays and filters Serilog events.
- No UI-thread stalls under continuous metric updates.

## Out of Scope
- External APM/telemetry export.
- Alerting/notifications.
- Historical trend analytics beyond current session/workspace.

## Risks
- LiveCharts2 performance with high-frequency updates.
- Serilog sink buffering/threading for the viewer.
- Timeline reconstruction fidelity from persisted results.

## Future Improvements
- Custom dashboards / saved layouts.
- Cross-run comparison and trend history.
- Exportable reports (ties into S14).

## Checklist
- [x] Metrics stream aggregation + observable pipeline.
- [x] LiveCharts2 dashboard widgets.
- [x] Execution timeline + drill-down.
- [x] Run replay service + UI.
- [x] Serilog-backed log viewer with filtering.

## Implementation notes (delivered)
- **Unified run store** (chosen over per-source stores): the existing skeletal `Run`/`RunStep`
  entities were enriched (`RunSource`, target, `ParentStepId` nesting, timing, status code,
  request/response JSON snapshots) and a new `LogEventRecord` added. Schema bumped **8 → 9**;
  migration `AddRunHistory` adds the `LogEvents` table + the new `Runs`/`RunSteps` columns. New
  ports `IRunStore` / `ILogEventStore` with EF `RunStore` / `LogEventStore` (mirror
  `StressRunRepository`). See **ADR-0011**.
- **Recording** (`Application/Runs`): `IRunRecorder`/`RunRecorder` maps request/workflow/stress
  executions (pure `RunMapper`) into the tree and publishes to `IMetricsFeed`; best-effort (swallows
  store failures, no-op when no workspace). Wired into `RequestExecutionService`,
  `StressOrchestrator`, and `WorkflowEditorViewModel`.
- **Dashboard** (`Application/Dashboard` + `UI/…/Dashboard`): `IDashboardService`/`DashboardService`
  aggregates run headers into `DashboardSnapshot` (counts, success/failure rate, avg duration,
  timeline, slowest/most-called rankings, status distribution). Built-in LiveCharts2 widgets
  (Overview, Success Rate donut, Latency line, Slowest/Most-Called bars) are first-party
  `IDashboardWidget` implementations enumerated from DI; `DashboardViewModel` refreshes live off
  `IMetricsFeed` (marshalled to the UI thread).
- **Timeline** (`UI/…/Panels/TimelineViewModel`): run list + `RunStep` drill-down tree (nesting via
  `ParentStepId`); `IRunReplayService` re-drives request snapshots / re-runs workflows (stress not
  replayable).
- **Log viewer**: `WorkspaceDbLogSink` (buffered Serilog sink, bounded pre-open ring, batched flush
  every 2 s to the open workspace DB), wired in `App.xaml.cs` (container owns its lifetime).
  `LogViewerViewModel` filters by level/source/text over `ILogEventStore`.
- **Packaging**: LiveCharts2 enabled in `UI.csproj`; its net-framework-built transitive deps
  (SkiaSharp.Views.WPF / OpenTK) raise `NU1701`, suppressed at project scope for the charting
  consumers (UI, Host, UI.Tests) only.
- **Tests**: `Application.Tests` (RunRecorder, DashboardService, RunReplayService) and
  `Infrastructure.Tests` (RunStore, LogEventStore round-trips). Full suite green (295 tests), build
  0 warnings.
