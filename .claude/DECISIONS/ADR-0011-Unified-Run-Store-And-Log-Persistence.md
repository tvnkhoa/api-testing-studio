# ADR-0011 — Unified run store + DB-backed application log

- **Status:** Accepted
- **Date:** 2026-07-20

## Context

Sprint 13 (Dashboard & Logging) needs a single source of truth for the observability surfaces —
the live Dashboard, the execution Timeline (with drill-down), and Replay — plus a filterable Log
Viewer. Before this sprint:

- Workflow runs were **not persisted** (`InMemoryWorkflowRunStore`); the `Run`/`RunStep` tables
  existed from `InitialCreate` but were skeletal (`RunStep` had only id/order/name/status) and
  nothing wrote to them.
- Stress runs were persisted (`StressRuns`/`StressMetrics`, Sprint 12) and request sends were
  persisted per-endpoint (`RequestHistoryEntry`, Sprint 06) — three unrelated shapes.
- Serilog wrote to console + rolling file only; there was no in-app, filterable log surface.

## Decision

1. **One unified run-log tree.** Enrich the existing `Run`/`RunStep` entities rather than adding
   per-source tables. `Run` gains `Source` (`RunSource`: Request/Workflow/Stress), target id/name,
   duration, and error; `RunStep` gains `ParentStepId` (Loop/Parallel nesting), kind, status code,
   timing, iteration, error, and request/response JSON snapshots (for Replay). Request, workflow, and
   stress executions all record into this one tree via the `IRunStore` port; the Dashboard, Timeline,
   and Replay read only from it. Request history and stress metrics remain as their own
   feature-specific stores (unchanged); the run tree is the cross-cutting observability model.

2. **Recording is best-effort, in the Application layer.** `IRunRecorder`/`RunRecorder` maps outputs
   (pure `RunMapper`) and saves via `IRunStore`, swallowing store failures and no-opping when no
   workspace is open, so telemetry never breaks the user's primary action. The Application layer
   stays free of a logging dependency, so the swallow is intentionally silent.

3. **Application log persists to the workspace DB.** A new `LogEvents` table (entity
   `LogEventRecord`, distinct from the per-run execution `LogEntry`) backs the Log Viewer. A buffered
   Serilog sink (`WorkspaceDbLogSink`) holds events in a bounded ring and flushes batches to the
   **currently open** workspace on a timer. Because Serilog is configured before the container exists
   and before any workspace is open, events emitted pre-open are buffered and flushed when a workspace
   opens; the viewer is scoped to the open workspace (consistent with "everything belongs to a
   Workspace"). Trade-off: application logs do not survive across workspace switches and a bounded
   number of pre-open events may be dropped — acceptable for diagnostics. A separate app-data log DB
   was considered and rejected as heavier and inconsistent with the workspace-scoped model.

4. **Schema 8 → 9**, migration `AddRunHistory` (new `LogEvents` table + new `Runs`/`RunSteps`
   columns). `Workspace.CurrentSchemaVersion` bumped accordingly.

5. **LiveCharts2 packaging.** The charting library's transitive `SkiaSharp.Views.WPF` / `OpenTK`
   ship .NET Framework assets and raise `NU1701` on `net10-windows`; they run correctly on .NET 10, so
   `NU1701` is suppressed at project scope for the charting consumers only (UI, Host, UI.Tests) rather
   than relaxing warnings-as-errors globally.

## Consequences

- Timeline/Replay/Dashboard share one query surface; adding a new run source means writing a `Run`
  tree, nothing more.
- Dashboard aggregation reads run headers only (cheap to recompute on every run-completed
  notification); per-step detail is loaded lazily for the Timeline drill-down.
- The `IDashboardWidget` plugin contract remains the identity/discovery seam; built-in widgets ship
  as first-party implementations enumerated from DI, so third-party widgets arrive the same way.
