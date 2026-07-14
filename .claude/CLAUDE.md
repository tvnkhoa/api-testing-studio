# CLAUDE.md — Project Constitution

> Single source of truth for **API Testing Studio**. Read this first, every time.
> Documentation is source code: update the docs **before** you change the code.

## What we are building

A **Workflow-first API Testing Platform** — a modern **.NET 10 + WPF** desktop application for
Backend Developers, QA Engineers, and DevOps. It works **100% offline** with **no cloud
dependency**. It is **not** a Postman clone; the differentiator is visual workflow automation,
role/permission testing, stress testing, and integrated test-case management.

Everything belongs to a **Workspace**: Services, Endpoints, Profiles, Environments, Variables,
Workflows, Test Cases, Dashboard, Logs, Attachments, Settings.

## Development philosophy

1. **Think before coding. Design before implementing.**
2. **Maintainability over short-term simplicity.** This product must evolve for years.
3. **Documentation-driven development.** Architecture changes are documented here first.
4. **Plugin-first.** Every capability is replaceable behind an abstraction; the core never
   references a concrete plugin.
5. **Never knowingly introduce technical debt. Never violate the architecture.**

## Core principles (non-negotiable)

- **Clean Architecture** with strict inward-only dependencies. See `ARCHITECTURE.md`.
- **SOLID, DRY, KISS, YAGNI**, composition over inheritance.
- **Dependency Injection** everywhere (`Microsoft.Extensions.DependencyInjection`).
- **MVVM** (CommunityToolkit.Mvvm). Business logic never lives in the UI. UI talks only to
  ViewModels. Avoid code-behind.
- **async/await** everywhere; no sync-over-async.
- **Immutable records** for domain/DTO types.
- **Secrets are always encrypted at rest** — never persist plaintext.
- **Offline only** — no telemetry, no network calls except those the user explicitly triggers.

## AI / contributor rules (the working loop)

For **every** task:

1. Read `CLAUDE.md`.
2. Read the related architecture documents (`ARCHITECTURE.md`, `CODING_STANDARDS.md`, relevant
   `FEATURES/*`, `API/*`, `UI/*`).
3. Read the current sprint in `TASKS/`.
4. Understand the existing implementation before proposing changes.
5. Propose an implementation plan. **Wait for confirmation if the architecture changes.**
6. Implement.
7. Build. Fix **all** warnings (the build treats warnings as errors).
8. Update documentation.

Never skip documentation. Never violate Clean Architecture. Prefer extensibility over shortcuts.

## Technology stack (authoritative)

| Concern | Choice |
|---|---|
| Framework | .NET 10 |
| Desktop UI | WPF |
| Architecture | Clean Architecture + MVVM |
| DI | Microsoft.Extensions.DependencyInjection / Hosting |
| Persistence | SQLite via EF Core |
| Logging | Serilog |
| Charts | LiveCharts2 (Sprint 13) |
| Workflow designer | Nodify (Sprint 09) |
| Docking | AvalonDock |
| JSON editor | Monaco via WebView2 (later sprint) |
| Icons/theme | Material Design |
| Serialization | System.Text.Json |
| Package format | `.apistudio` = ZIP(manifest.json + database.sqlite + attachments/) |

## Repository map

```
src/         Domain · Shared · Plugin.Abstractions · Application · Core · Infrastructure · UI · Host
plugins/     Import.* · Assertion.* · Runner.Stress · Export.ApiStudio
tests/       Domain.Tests · Application.Tests · Infrastructure.Tests · PluginHost.Tests
.claude/     THIS documentation (source of truth)
```

Build with `dotnet build ApiTestingStudio.slnx`. Run with
`dotnet run --project src/ApiTestingStudio.Host`. See `TASKS/Sprint-01.md` for the current state.
