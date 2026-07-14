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
- [ ] Shell window with AvalonDock + menu + toolbar + status bar.
- [ ] Theme manager (light/dark) with persistence.
- [ ] Layout persistence service.
- [ ] Panel registration abstraction.
- [ ] Recent-workspaces UI wired to Sprint 02 service.
