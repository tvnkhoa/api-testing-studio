# Dashboard

> UX rationale & benchmark (Grafana): see `../UI_BENCHMARK.md` (Feature Mapping, Analyze Dashboard journey).

## Overview

The **Dashboard** is a workspace-level analytics surface built on **LiveCharts2**. It aggregates
run history and endpoint activity into a grid of widgets so a user can see, at a glance, how their
APIs are behaving across recent runs. It is hosted in an AvalonDock **document** pane (it can also
surface as a tool pane); it reads persisted run data and refreshes as runs complete. Fully offline —
no telemetry, no network.

## Scope / Capabilities

Built-in widgets (each a LiveCharts2 chart):

- **Request count** — total requests over a selected window (cards + bar).
- **Success / failure rate** — pass vs fail as a ratio (pie/donut) driven by `RunStatus`.
- **Timeline** — requests / durations over time (line/area series).
- **Slowest endpoints** — top-N by latency (horizontal bar).
- **Most-called endpoints** — top-N by request count (horizontal bar).
- **Error distribution** — failures grouped by status/assertion outcome (bar/pie).

Widgets are arranged in a responsive grid; time-window and workspace filters drive all widgets.

## Extensibility (plugin contract)

Widgets are contributed through the **`IDashboardWidget`** plugin contract
(`ApiTestingStudio.Plugin.Abstractions.Ui`): `WidgetId` (stable id) and `Title` (display name).
The plugin host discovers implementations via `IPluginModule`; the Dashboard enumerates the
registered widgets and hosts each one. The full view/data contract for a widget is fleshed out in
this sprint — Phase 1 defines identity/metadata only. The built-in widgets ship as first-party
`IDashboardWidget` implementations, so third-party analytics are added the same way as built-ins.

## Domain & Contracts

Reads from the persisted run tree (`Run` / step records — see `Logging.md`) and the Service/Endpoint
catalog. The Dashboard is a consumer: aggregation queries live in the Application layer; the UI
binds to view models only, holding no business logic.

## UI

- MVVM (CommunityToolkit.Mvvm). `DashboardViewModel` exposes a collection of widget view models and
  the active filters; each widget view model provides LiveCharts2 `ISeries` / axes.
- Material Design theming; charts honour the active light/dark theme and accent (see `Themes.md`).

## Sprint

- **Sprint 13** — dashboard, LiveCharts2 widgets, timeline, monitoring, logs, replay.
- Depends on run history from Sprints 06/08/11/12 and the Sprint 03 plugin registry.

### Delivered (Sprint 13)

`DashboardViewModel` (an AvalonDock **document** opened from View → Dashboard) hosts a responsive
`WrapPanel` grid of widget view models, each a first-party `DashboardWidgetViewModel`
(`IDashboardWidget` + a UI-side `IDashboardWidgetContent.Update(DashboardSnapshot)`), rendered by
implicit `DataTemplate`s. It enumerates the registered `IDashboardWidget`s from the container — the
same seam a plugin widget would arrive through — and refreshes live off `IMetricsFeed`. The full
widget data contract for plugin authors (exposing the read-model across the plugin boundary) is a
future extension; this sprint ships the built-ins first-party. See ADR-0011.

## Open Questions / Future

- User-customisable widget layout (add/remove/resize, persisted with the dock layout).
- Cross-run comparison and export of dashboard snapshots.
- Drill-through from a widget into the underlying runs/logs.
