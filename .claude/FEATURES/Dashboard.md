# Dashboard

## Overview

The Dashboard gives a **realtime, at-a-glance view** of API activity within a Workspace. As
requests execute — whether from a single endpoint call, a workflow run, or a stress test — the
dashboard aggregates their results and updates its widgets live. It is a read-only analytical
surface built entirely from persisted run/response data; it triggers no traffic of its own.

## Scope / Capabilities

Metrics and widgets:

- **Request Count** — total requests over the selected window.
- **Success Rate** / **Failure Rate** — proportion of 2xx (success) vs. error/timeout responses.
- **Average Response Time** — mean latency across the window.
- **Timeline** — requests and latency plotted over time.
- **Slowest Endpoints** — ranked by response time.
- **Most Called Endpoints** — ranked by request count.
- **Error Distribution** — breakdown by status class / error type.

Supporting behaviour:

- Time-window and environment filters (Development, QA, Staging, Production).
- Live refresh as new responses are recorded; no manual reload.
- Fully offline — all figures are computed from the local SQLite store.

## Domain & Contracts

Domain (`ApiTestingStudio.Domain`) — the dashboard reads existing execution records rather than
introducing new persisted entities:

- `Request`, `Response` (status code, latency, timestamp, endpoint id).
- `Run` / `RunStep` for run-level grouping (see `Logging.md`).

Read-model / aggregate records (immutable) surfaced to the UI, e.g. `DashboardSnapshot`,
`EndpointStat`, `ErrorBucket`, `TimelinePoint`.

Plugin contract (`ApiTestingStudio.Plugin.Abstractions`):

- `IDashboardWidget` — each widget is a plugin: metadata (title, size) plus a method that produces
  its view-model from a query over the response store. New widgets are added as plugins and
  discovered through the plugin registry.

Aggregation runs through an Application service that queries `IStorageProvider`; the UI never
touches EF Core directly.

## UI

- Charts rendered with **LiveCharts2** (line chart for Timeline, bars for rankings, pie/donut for
  Error Distribution, KPI cards for the scalar metrics).
- A responsive widget grid hosted in an **AvalonDock** pane; each `IDashboardWidget` binds to its
  own view-model (CommunityToolkit.Mvvm).
- Material Design styling; theme-aware.

## Sprint

- **Sprint 13** — dashboard widgets, aggregation queries, and LiveCharts2 integration.

### Delivered (Sprint 13)

Aggregation runs in `IDashboardService`/`DashboardService`, reading run headers from the unified
`IRunStore` (see `Logging.md`, ADR-0011) into an immutable `DashboardSnapshot` (counts,
success/failure rate, average duration, timeline, slowest / most-called rankings, status
distribution). The built-in widgets (Overview, Success Rate donut, Latency line, Slowest /
Most-Called bars) are first-party `IDashboardWidget` implementations enumerated from DI; the
`DashboardViewModel` refreshes them live off `IMetricsFeed` as runs complete, marshalled to the UI
thread. Charts use **LiveCharts2**.

## Open Questions / Future

- User-arrangeable / savable dashboard layouts.
- Percentile latency widgets (P95/P99) shared with the stress-testing metrics.
- Export dashboard snapshot to image / report.
- Configurable alert thresholds (visual only, still offline).
- Retention/rollup policy for very large response histories.
