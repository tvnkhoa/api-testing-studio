# ADR-0001 — Use WPF on .NET 10 for the desktop UI

- **Status:** Accepted
- **Date:** 2026-07-14

## Context

API Testing Studio is a Windows-first, fully-offline desktop application targeting Backend
Developers, QA, and DevOps. It needs a mature, richly-controlled desktop UI with strong docking,
data-binding (MVVM), and access to the .NET ecosystem (EF Core, Serilog). The team is
.NET-centric.

## Decision

Build the UI with **WPF on .NET 10**, using **MVVM** (CommunityToolkit.Mvvm), **AvalonDock** for
docking, and Material Design theming. The desktop shell is a `WinExe` composition-root project
(`Host`); reusable views/ViewModels live in a `net10.0-windows` UI library.

## Alternatives considered

- **WinUI 3 / MAUI** — less mature docking/third-party control ecosystem for this use case.
- **Avalonia** — cross-platform, but Windows-first + the richest WPF control/docking ecosystem
  (AvalonDock, Nodify, LiveCharts2) made WPF the pragmatic choice.
- **Electron/web** — contradicts the offline-native, low-overhead desktop goal.

## Consequences

- Windows-only for now. Cross-platform would require re-hosting the UI (the Clean Architecture
  boundaries keep Domain/Application/Core/Infrastructure UI-agnostic, limiting the blast radius).
- We gain AvalonDock, Nodify, LiveCharts2, and Monaco-via-WebView2.
- UI stays behind MVVM so business logic remains portable.
