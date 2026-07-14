# UI_GUIDELINES.md

## WPF + MVVM rules

- **MVVM only.** Views bind to ViewModels; ViewModels are injected via DI. No business logic in
  Views or code-behind. Code-behind is limited to view-only concerns (e.g. focus, close).
- ViewModels derive from `ObservableObject` and use CommunityToolkit.Mvvm source generators:
  `[ObservableProperty]`, `[RelayCommand]`. **Keep ViewModels small.**
- Views live in `ApiTestingStudio.UI` (controls/windows) and, for the shell, in
  `ApiTestingStudio.Host`. Feature ViewModels live in `ApiTestingStudio.UI/ViewModels`.
- Never reference Infrastructure from UI. Depend on Application/Core ports.

## XAML conventions

- One control/window per file; `x:Class` matches the namespace.
- Prefer `{Binding}` with typed DataContext; enable compiled bindings where practical.
- No inline business logic in XAML. Use commands and value converters.
- Resource dictionaries per theme/module; merge at App level.
- Element naming: `PascalCase` `x:Name` only when the view genuinely needs it.

## Docking & navigation

- **AvalonDock** `DockingManager` hosts the shell. Two pane kinds: **document panes** (editors,
  designers, dashboards) and **tool panes** (Explorer, Logs, Properties).
- Layout is persisted and restored per workspace (Sprint 04). A sensible default layout ships
  with the app.
- Navigation is document-oriented: opening an endpoint/workflow opens or focuses a document pane.
- The Phase 1 shell (`Host/MainWindow.xaml`) is an **empty** DockingManager with a menu bar and
  status bar — panes are added in their feature sprints.

## Theme, spacing, icons

- **Material Design** theme (light/dark) with an accent color. Centralize colors, brushes, and
  typography in theme resource dictionaries (Sprint 04).
- Spacing scale (4-based): 4 / 8 / 12 / 16 / 24 / 32. Use consistent margins/paddings from the
  scale; avoid arbitrary values.
- Icons: Material Design icon set. Use semantic, consistent iconography.

## Controls

- Prefer built-in WPF + Material Design styled controls. Introduce third-party controls
  (Nodify for the workflow canvas, LiveCharts2 for charts, Monaco via WebView2 for JSON) only in
  the sprint that needs them, and keep their packages referenced by `UI`/`Host` only.

## Accessibility

- All actionable controls are keyboard-reachable with a sensible tab order.
- Provide `AutomationProperties.Name` / tooltips for icon-only buttons.
- Respect system font scaling; use relative sizing. Maintain sufficient contrast in both themes.
- Never convey state by color alone (pair color with icon/text — e.g. Pass/Fail).

## Async & responsiveness

- UI never blocks. Long operations run async with progress/cancellation; the shell stays
  interactive. Marshal back to the UI thread only where required.
