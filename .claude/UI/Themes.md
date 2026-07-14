# Themes

## Overview

The application uses a **Material Design** visual language for theming and iconography across the
WPF shell. Theming is centralised as merged `ResourceDictionary` resources so every surface —
docking shell, Explorer, Workflow Designer, Dashboard — reads from one palette and one spacing
scale. Everything is offline; no web fonts or remote assets.

## Scope / Capabilities

- **Light / dark modes** — a full light and dark palette. The active mode is a user setting,
  persisted with the workspace/app settings and applied at startup by swapping the theme dictionary.
- **Accent colours** — a selectable accent (primary) colour drives buttons, selection, focus, and
  chart highlights; a secondary accent is available for emphasis. Semantic colours encode state:
  success (pass), error (fail), warning, and info — reused by the Explorer verb badges, run status
  on Workflow nodes, and Dashboard charts.
- **Spacing scale** — a consistent spacing token set (e.g. 4 / 8 / 12 / 16 / 24 / 32 px) exposed as
  named resources, so margins, padding, and grid gutters stay uniform across views.
- **Typography** — a Material type ramp (headings, body, caption) as shared text styles.
- **Iconography** — Material Design icons throughout (menu, tree nodes, node palette, toolbars);
  icons are vector and tint to the current theme/accent.

## Structure

- Theme resources are merged `ResourceDictionary` files (base tokens + light/dark overrides +
  control styles), merged in `App.xaml` at startup. Colours are `Color`/`Brush` resources keyed by
  semantic name (not raw hex at call sites), so a re-theme touches only the dictionaries.
- All controls consume brushes/styles by key; no view hard-codes colours or sizes. This keeps
  light/dark and accent changes a pure resource swap with no per-view code.

## UI

- Applied consistently to the AvalonDock shell (`DockLayout.md`), Explorer, Workflow Designer,
  and Dashboard. LiveCharts2 series/axes read the same semantic brushes so charts match the theme.
- Theme selection is exposed through a settings surface; switching is live where practical.

## Sprint

- **Sprint 04** — theme brought up alongside the shell (Material Design, light/dark, accent,
  spacing scale, iconography). Chart theming is consumed by the Dashboard in Sprint 13.

## Open Questions / Future

- Follow OS light/dark preference automatically.
- User-defined custom accent colours.
- High-contrast / accessibility theme variant.
