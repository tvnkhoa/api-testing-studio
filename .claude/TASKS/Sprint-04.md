# Sprint 04 — Shell UI

## Goal
Build the WPF application shell: an AvalonDock-based docking workspace with toolbar, status bar, Material Design theming, and persisted layout — the visual frame every feature docks into.

## Scope
- Fill out the Host's empty AvalonDock shell into a real main window.
- Main menu + toolbar (New/Open/Save workspace, view toggles).
- Status bar (workspace name, connection/offline indicator, background-task status).
- Theme system: light/dark Material Design, runtime switch.
- Layout persistence (dock positions, floating windows, sizes) per workspace/user.
- Recent-workspaces UI wired to Sprint 02 `IRecentWorkspacesService`.

## Requirements
- MVVM throughout using CommunityToolkit.Mvvm; no code-behind logic beyond view wiring.
- Dockable panels register via a `IToolWindow`/panel abstraction for later features.
- Theme choice and layout persist across restarts.
- Fully keyboard-navigable menus and commands.

## Architecture Impact
- Introduces the UI shell composition root and navigation/docking service.
- Establishes the panel-registration pattern consumed by Sprints 05-13.
- Wires DI/Hosting generic host into WPF App startup.

## Projects (which solution projects change)
- UI — shell window, view models, theming, dock services, controls.
- Host — App startup, host builder, main window bootstrap.
- Application — layout/settings persistence contracts.
- Infrastructure — layout/settings store implementation.
- Tests: Application.Tests (view-model logic).

## Classes
- `ShellWindow`, `ShellViewModel`, `MainMenuViewModel`, `ToolbarViewModel`, `StatusBarViewModel`.
- `DockManagerService`, `ThemeManager`, `LayoutPersistenceService`.
- `ToolWindowBase` / panel view-model base.
- `RecentWorkspacesMenuViewModel`.

## Interfaces
- `IDockManager`, `IToolWindow`, `IThemeManager`.
- `ILayoutPersistenceService`, `IShellNavigationService`.
- `IStatusBarService` (background-task/notification sink).

## Database Changes
- None in workspace DB. (Layout/theme stored in app-settings store; uncertain whether per-workspace layout later needs a DB row.)

## Plugin Changes
- None. (Plugins may later contribute panels via `IToolWindow`; not this sprint.)

## UI Changes
- New main shell window with docking, menu, toolbar, status bar.
- Light/dark theme toggle.
- Recent-workspaces menu and open/create flow.

## Acceptance Criteria
- App launches to the shell; open/create workspace works end-to-end.
- Panels can be docked, floated, and rearranged; layout persists across restart.
- Theme toggles at runtime and is remembered.
- Status bar reflects current workspace and offline state.

## Out of Scope
- Feature panels' content (Explorer, Runner, etc. — later sprints).
- Plugin-contributed UI.
- Localization.

## Risks
- AvalonDock layout serialization brittleness across versions/DPI.
- WebView2 (Monaco) init timing within docked panels (surfaces in later sprints).
- Material Design + AvalonDock theme conflicts.

## Future Improvements
- Command palette / quick-open.
- Customizable toolbar and per-user workspaces.
- High-contrast/accessibility themes.

## Checklist
- [x] Shell window with AvalonDock + menu + toolbar + status bar. (`Host/ShellWindow.xaml`, `UI/ViewModels/ShellViewModel`, `MainMenuViewModel`, `ToolbarViewModel`, `StatusBarViewModel`)
- [x] Theme manager (light/dark) with persistence. (`UI/Services/ThemeManager` + `IThemeManager`, `Application/Settings/AppSettings`, `Infrastructure/Settings/AppSettingsService`)
- [x] Layout persistence service. (`UI/Services/DockManagerService` + `IDockManager`, `Application/Abstractions/ILayoutPersistenceService`, `Infrastructure/Settings/LayoutPersistenceService`)
- [x] Panel registration abstraction. (`UI/ViewModels/Panels/PanelViewModel` → `ToolPanelViewModel`/`DocumentPanelViewModel`, implicit templates in `Resources/PanelTemplates.xaml`)
- [x] Recent-workspaces UI wired to Sprint 02 service. (`UI/ViewModels/RecentWorkspacesMenuViewModel` over `IRecentWorkspacesService`)

## Outcome (2026-07-14)
- Decisions: single **global per-user** dock layout (per-workspace deferred) and a **manual
  light/dark** theme toggle (follow-OS deferred) — see `DECISIONS/ADR-0008-Shell-UI-Layout-Theme-Persistence.md`.
- Layout is serialized with AvalonDock's `XmlLayoutSerializer` to an opaque string and stored via a
  string-only `ILayoutPersistenceService` port; theme swaps the Material Design base theme via
  `PaletteHelper` and persists through `IAppSettingsService`. Both stores are JSON/XML files under the
  app-data directory, mirroring Sprint 02's `RecentWorkspacesService`.
- Deviation from the plan's "Application.Tests (view-model logic)": the view models live in the
  net10.0-windows `UI` project, so a dedicated `tests/ApiTestingStudio.UI.Tests` project hosts the
  view-model tests (kept WPF-free with fakes). Settings/layout stores are covered in
  `Infrastructure.Tests`.
- `MainWindow`/`MainViewModel` were replaced by `ShellWindow`/`ShellViewModel`; App startup now calls
  `AddUi()`, applies the persisted theme before showing the shell, and restores the saved layout on
  load / saves it on close.
- Verified end-to-end: app launches to the shell with 10 plugins active and no workspace open;
  closing the shell writes `dock-layout.xml` with pane `ContentId`s preserved. Build clean
  (0 warnings); all 70 solution tests green (48 prior + 22 new).
