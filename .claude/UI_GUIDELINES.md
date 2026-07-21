# UI_GUIDELINES.md

> For the UX rationale and the benchmark-derived design system behind these rules, see
> `UI_BENCHMARK.md`. This file is the **how** (WPF/MVVM/XAML rules); `UI_BENCHMARK.md` is the **why**.

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

## Design tokens (`Themes/Tokens.xaml`)

Single source for colour, spacing, and type. **Never put a raw hex, a literal margin/padding, or a
literal `FontSize` at a call site** — reference a token so a re-theme touches one file. `Tokens.xaml`
and `Controls.xaml` merge at App level, after the Material base theme. Off-scale values are
normalised to the nearest token; genuinely two-axis margins get a named composite token here rather
than being inlined.

**Colour** — semantic keys only; each has a `.Color` and a matching `.Brush`:

| Token | Use |
|---|---|
| `Semantic.Success` / `Error` / `Warning` / `Info` | pass / fail / warn / info state — always paired with text or icon, never colour alone |
| `Semantic.Muted` | neutral "not applicable" (e.g. a skipped assertion) |
| `Text.OnAccent` | text on a saturated fill (HTTP verb chip); white in both themes |
| `Border.Divider` | subtle 1px separators (panel edges, list-row rules) |
| `Border.Card` | 1px outline around raised cards (dashboard widgets, wizard steps) |
| `Surface.Muted` / `Surface.MutedStrong` | subtle grey panel / callout fills, two strengths |

For secondary/hint text prefer the theme-aware system brush
`{DynamicResource {x:Static SystemColors.GrayTextBrushKey}}` (adapts to light/dark) over a fixed grey.

**Spacing** — the 4-based scale (`4 / 8 / 12 / 16 / 24 / 32`) as `Spacing.{Xs,Sm,Md,Lg,Xl,Xxl}`
(`Double`) plus a matching `.Thickness` for uniform margins/paddings (a `Double` can't fill a
`Thickness`). Directional variants (`.Left/Top/Right/Bottom.Thickness`), axis pairs
(`.Horizontal/Vertical.Thickness`), and two composites (`Spacing.Separator.Thickness`,
`Spacing.SubItemIndent.Thickness`) are added on demand — never inline a literal `Thickness`.

**Typography** — the ramp `Typography.{Headline 28, Title 20, Body 14, Caption 12, Overline 10}`.
Bind `FontSize` to a ramp token; `Overline` is the micro step for uppercase chips/badges.

**Controls** — button chrome via `Button.Padding` (standard) and `Button.Padding.Compact` (inline
row actions), consumed by the control-style layer below.

## Controls

- Prefer built-in WPF + Material Design styled controls. Introduce third-party controls
  (Nodify for the workflow canvas, LiveCharts2 for charts, Monaco via WebView2 for JSON) only in
  the sprint that needs them, and keep their packages referenced by `UI`/`Host` only.
- **Control-style layer** (`Themes/Controls.xaml`): shared control chrome so views never set
  padding/sizing inline. The implicit `Button` style is `BasedOn` the Material implicit button
  (`{x:Type Button}`) and only standardises `Padding` (Material chrome preserved; `ToolBar` buttons
  are styled by WPF via `ToolBar` and are unaffected). Use the keyed `CompactButton` style for tight
  inline/row actions (e.g. a list-row ✕). New control defaults belong here, not inline per view.

## Accessibility

- All actionable controls are keyboard-reachable with a sensible tab order.
- Provide `AutomationProperties.Name` / tooltips for icon-only buttons, and for any input whose
  label is a separate adjacent element (form fields, filters, splitters, canvases) so it is
  announced without a programmatic label association.
- Respect system font scaling; use relative sizing. Maintain sufficient contrast in both themes.
- Never convey state by color alone (pair color with icon/text — e.g. Pass/Fail).

## Async & responsiveness

- UI never blocks. Long operations run async with progress/cancellation; the shell stays
  interactive. Marshal back to the UI thread only where required.
