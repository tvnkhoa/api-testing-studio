# ADR-0008 — Shell UI: layout & theme persistence

- **Status:** Accepted
- **Date:** 2026-07-14

## Context

Sprints 01–03 left the WPF `Host` with an empty AvalonDock shell (`MainWindow` bound to a
plugin-count-only `MainViewModel`). Sprint 04 builds the real application shell — docking workspace,
menu, toolbar, status bar, Material Design theming, and persisted layout — that every later feature
(Explorer S05, Runner S06, Workflow S09, Dashboard S13) docks into. The shell must obey Clean
Architecture (`UI` never references `Infrastructure`; only `Host` composes concrete adapters), stay
MVVM (no business logic in code-behind), and persist user choices across restarts while remaining
100% offline.

Two open questions from `UI/DockLayout.md` and `UI/Themes.md` had to be settled: per-workspace vs
global layout, and manual vs OS-following theme.

## Decision

1. **Global per-user layout; manual light/dark theme.** For this sprint the dock layout is a single
   per-user arrangement and the theme is a manual toggle. Per-workspace layouts and follow-OS theme
   are deferred (both already listed as "future" in the UI docs). This keeps save-on-close simple
   (no dependence on whether a workspace is open) and the theme model explicit.

2. **String-only persistence ports in Application, file adapters in Infrastructure.**
   `IAppSettingsService` (reads/writes an immutable `AppSettings` record carrying `ThemeMode`) and
   `ILayoutPersistenceService` (reads/writes an **opaque layout string** — it never parses it) live
   in `Application`. Their file-backed implementations (`AppSettingsService` → `app-settings.json`,
   `LayoutPersistenceService` → `dock-layout.xml`) live in `Infrastructure` under the app-data
   directory, mirroring Sprint 02's semaphore-guarded, corrupt-tolerant `RecentWorkspacesService`.
   This keeps EF/WPF/file types out of `Application` and lets `UI` depend only on ports.

3. **Docking bridged by a UI service, not code-behind.** `IDockManager`/`DockManagerService` (UI)
   owns AvalonDock's `XmlLayoutSerializer`: it serializes the live `DockingManager` to a string and
   delegates storage to `ILayoutPersistenceService`. The shell window hands its `DockingManager` to
   `Attach` (the only view wiring), which also snapshots the XAML-generated default so **Reset
   Layout** can restore it. On restore, panes are re-associated with the live view models bound to
   `DocumentsSource`/`AnchorablesSource` by matching `ContentId`; unknown panes (e.g. a removed
   plugin) are dropped, and a layout from an incompatible AvalonDock version falls back to the
   default instead of crashing.

4. **Theme via Material Design `PaletteHelper`.** `IThemeManager`/`ThemeManager` (UI) swaps the base
   (light/dark) theme on the running application's merged dictionaries and persists the choice
   through `IAppSettingsService`. The persisted theme is applied at startup **before** the window is
   shown, so there is no flash of the wrong theme.

5. **Panel-registration seam.** `PanelViewModel` (with `ToolPanelViewModel` /
   `DocumentPanelViewModel`) is the base every dockable pane derives from; the shell exposes
   `Documents`/`Tools` collections bound to AvalonDock, and implicit `DataTemplate`s in
   `Resources/PanelTemplates.xaml` map each pane view model to its view. Later sprints add a feature
   panel by adding a view model + view + one template; plugins contribute panes through the existing
   `IToolWindow` contract, materialised the same way.

6. **DI + startup.** A new `AddUi()` extension registers the shell/panel view models and the
   WPF-facing services (theme, docking, status bar, file dialog) behind interfaces; only `Host`
   calls it. `MainWindow`/`MainViewModel` are replaced by `ShellWindow`/`ShellViewModel`.

## Consequences

- `UI` gains WPF-facing services (`IThemeManager`, `IDockManager`, `IStatusBarService`,
  `IFileDialogService`) behind interfaces, so view models stay unit-testable with fakes — verified by
  the new `tests/ApiTestingStudio.UI.Tests` project.
- The layout payload is intentionally opaque to `Application`/`Infrastructure`; its schema is
  AvalonDock's and can change with the library. A corrupt/incompatible layout degrades to the
  default rather than failing.
- Global layout means all workspaces share one arrangement. Moving to per-workspace layouts later is
  additive: key the layout store by workspace location; no port signature needs to change beyond an
  optional key.
- A workspace is a single self-contained SQLite file; the shell adopts the `.atsdb` extension for the
  open/create dialogs (`FileDialogService`). The storage provider itself remains
  extension-agnostic.
- Chart/theme consumers (Dashboard, S13) will read the same semantic brushes/tokens defined in
  `UI/Themes/Tokens.xaml`.
