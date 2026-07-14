# Dock Layout

## Overview

The application shell is a single **AvalonDock** `DockingManager` that hosts every workspace
surface. It separates **document panes** (the primary editing surfaces the user works in) from
**tool panes** (supporting side panels), and persists the arrangement so the app reopens exactly as
the user left it. The shell lives in `src/ApiTestingStudio.Host/MainWindow.xaml`.

## Current state (Phase 1)

`MainWindow.xaml` today is a `DockPanel` with an application **menu** (top), a **status bar**
(bottom), and an **empty** `DockingManager` containing a single `LayoutDocumentPane`. The window
`Title` and `StatusMessage` bind to `MainViewModel`. Menu items (New/Open Workspace, Explorer,
Dashboard, Logs, About) are present but disabled — they light up in their own sprints.
**Sprint 04** builds the docking shell out from here.

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

## Open Questions / Future

- Per-workspace layouts vs one global layout.
- Named layout presets (e.g. "Design", "Run", "Analyse").
- Restoring open documents (not just pane structure) across sessions.
