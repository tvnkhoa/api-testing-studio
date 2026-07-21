# Dock Layout

> UX rationale & recommended layout (Rider/VSCode/VS, activity rail): see `../UI_BENCHMARK.md` (Dock Layout, Navigation Flow).

## Overview

The application shell is a single **AvalonDock** `DockingManager` that hosts every workspace
surface. It separates **document panes** (the primary editing surfaces the user works in) from
**tool panes** (supporting side panels), and persists the arrangement so the app reopens exactly as
the user left it. The shell lives in `src/ApiTestingStudio.Host/MainWindow.xaml`.

## Current state (Sprint 04 — implemented)

The shell lives in `src/ApiTestingStudio.Host/ShellWindow.xaml`: a `DockPanel` with a **menu** and
**toolbar** (top), a **status bar** (bottom), and a `DockingManager` whose
`DocumentsSource`/`AnchorablesSource` bind to `ShellViewModel.Documents` / `ShellViewModel.Tools`.
Each pane's view model derives from `PanelViewModel` (`ToolPanelViewModel` /
`DocumentPanelViewModel`) and is rendered by implicit `DataTemplate`s in
`src/ApiTestingStudio.UI/Resources/PanelTemplates.xaml`. Placeholder Explorer (tool, left) and Logs
(tool, bottom) panes plus a Welcome document exercise docking; feature content lands in later
sprints. Code-behind is limited to view wiring: it hands the `DockingManager` to `IDockManager` on
load, restores the saved layout, and saves it on close.

Layout is serialized with `XmlLayoutSerializer` to an opaque string via
`DockManagerService`/`IDockManager` (UI) and stored by `ILayoutPersistenceService` (Application) →
`LayoutPersistenceService` (Infrastructure) as a **single global per-user** `dock-layout.xml` under
the app-data directory. **Reset Layout** restores the XAML default snapshot captured on attach.
Per-workspace layouts remain a future item. See
`DECISIONS/ADR-0008-Shell-UI-Layout-Theme-Persistence.md`.

## Runner (Sprint 06 — implemented)

The **API Runner** is the first real document pane (`ApiRunnerViewModel : DocumentPanelViewModel`,
`ContentId = "document.runner"`, rendered via a `DataTemplate` in `PanelTemplates.xaml`). Selecting
an endpoint in the Explorer publishes `EndpointSelected`; `ShellViewModel` opens-or-focuses the
single shared Runner pane and the runner loads that endpoint. Its body/response editors are hosted
by Monaco in a WebView2 (see `UI/Runner.md` and `DECISIONS/ADR-0009-*`). A per-endpoint history
list supports replay. One reused pane (not one tab per endpoint) — see `UI/Runner.md`.

## Scope / Capabilities

- **Document panes** (`LayoutDocumentPane`) — tabbed editing surfaces: Runner, Workflow Designer
  (`WorkflowDesigner.md`), Dashboard (`Dashboard.md`). Multiple documents, closeable, re-orderable.
- **Tool panes** (`LayoutAnchorablePane`) — dockable/floating/auto-hide panels: Explorer
  (`Explorer.md`, left), Logs (bottom), and any plugin-contributed `IToolWindow`.
- **Layout persistence / restore** — the `DockingManager` layout is serialised with AvalonDock's
  `XmlLayoutSerializer` to a per-user file and reloaded on startup; unknown panes (e.g. a plugin no
  longer present) are dropped gracefully during deserialisation.
- **Default layout** — a built-in arrangement (Explorer left, documents centre, Logs bottom) applied
  on first run or via a "Reset Layout" command when no saved layout exists or it fails to load.

## Extensibility

Plugin tool windows are contributed via **`IToolWindow`** (`ToolWindowId`, `Title`) and dashboard
widgets via **`IDashboardWidget`**. The shell enumerates the plugin registry and materialises a tool
pane per registered `IToolWindow`. First-party Explorer/Logs/Dashboard surfaces follow the same
contract, keeping built-in and plugin panes uniform.

## UI

- MVVM (CommunityToolkit.Mvvm). A `ShellViewModel` exposes `Documents` and `Tools` collections bound
  to the `DockingManager` via `DocumentsSource` / `AnchorablesSource`; each pane binds to its own
  view model. Layout save/load is a thin service invoked from the view model, not code-behind.
- Material Design theming applied to the manager, tabs, and splitters (see `Themes.md`).

## Sprint

- **Sprint 04** — WPF shell, AvalonDock, toolbar, status bar, theme, layout persistence.
- **Sprint 06** — first document pane: the API Runner (`document.runner`), opened/focused from the
  Explorer's endpoint selection.

## Open Questions / Future

- Per-workspace layouts vs one global layout.
- Named layout presets (e.g. "Design", "Run", "Analyse").
- Restoring open documents (not just pane structure) across sessions.
