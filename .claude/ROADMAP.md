# ROADMAP.md

## Vision

A Workflow-first, fully-offline API testing platform for Backend Developers, QA, and DevOps.
Beyond request/response testing, it delivers visual workflow automation, role/permission
testing, stress testing, and integrated test-case management — all in one maintainable,
plugin-extensible .NET 10 + WPF desktop product.

## Milestones

| Sprint | Title | Outcome |
|---|---|---|
| 00 | Architecture Validation & Spike | Architecture approved; contracts + `.apistudio` structure defined; ADRs; dependency graph validated |
| 01 | Foundation | Buildable solution: Clean Architecture, DI, logging, plugin host, empty shell, storage skeleton, tests. No business features. |
| 02 | Workspace Storage | SQLite/EF Core workspace lifecycle, metadata, recent workspaces, package metadata |
| 03 | Plugin Infrastructure | Hardened loader/registry, version compatibility, lifecycle, dynamic loading |
| 04 | Shell UI ✅ | AvalonDock shell, toolbar, status bar, theme, layout persistence (global per-user layout + manual light/dark) |
| 05 | Service Explorer ✅ | Service tree, endpoint CRUD, search, context menu |
| 06 | API Runner ✅ | Request builder, response viewer, headers, timing, history + replay; first Monaco/WebView2 host |
| 07 | Import System ✅ | cURL / OpenAPI / Swagger / Scalar / Postman import + auto-detection + wizard |
| 08 | Workflow Engine ✅ | Execution engine: nodes, edges, context, loop, parallel, delay, condition |
| 09 | Workflow Designer ✅ | Nodify canvas, drag & drop, zoom, minimap, undo/redo, visual mapping |
| 10 | Profiles & Environments ✅ | Identity profiles, AES/DPAPI secret storage, variables, environments |
| 11 | Assertions & Test Cases ✅ | Assertions (JSON/regex/schema), test suites, reporting |
| 12 | Stress Runner ✅ | Sequential/loop/concurrent runs, TPS/RPS, P95/P99 metrics |
| 13 | Dashboard & Logging ✅ | Live dashboard, charts, timeline, replay, monitoring, logs |
| 14 | Packaging & Polish ✅ | `.apistudio` package, backup/recovery, performance, optimization |

> **Status (2026-07-22):** All sprints 00–14 are delivered and the solution builds clean. Sprint 15
> was a frozen-development product review (`TASKS/Sprint-15/`); Sprint 16 (Consolidation Phase 1)
> then landed the trust/first-mile fixes: workflow/test/stress/replay runs now seed the active
> environment's variables (unresolved `{{tokens}}` warn instead of failing silently), Profile
> "Run As" is reachable (toolbar switcher + Runner + workflow node), the Runner's Cancel works, a
> real first-run Welcome with CTAs plus a programmatic sample workspace exist, and new workspaces are
> seeded with a default environment + `baseUrl`. The **Executable Scenarios harness** (FlaUI, item 8)
> is built under `tests/ApiTestingStudio.Scenarios` — an on-demand UI-automation regression net that
> screenshots the headline journeys (run with `ES_RUN=1`).
> Remaining post-14 gaps: `Switch`/`Variable` workflow nodes and visual **edge** data-mapping are not
> yet implemented (deferred to Sprint 17+); OpenAPI import maps method/path/query/headers/body;
> Postman environment import and an Attachments UI are outstanding.

Detailed per-sprint plans live in `TASKS/Sprint-00.md … Sprint-14.md`.

## Sprint planning conventions

Each sprint document follows the standard template: Goal, Scope, Requirements, Architecture
Impact, Projects, Classes, Interfaces, Database Changes, Plugin Changes, UI Changes, Acceptance
Criteria, Out of Scope, Risks, Future Improvements, Checklist.

Sprint working agreement (from the roadmap): read all `.claude/` docs and prior sprints; follow
Clean + Plugin Architecture; never break backward compatibility; update docs when architecture
changes; produce an implementation plan before coding; keep the solution buildable; prefer
extensibility over shortcuts; verify acceptance criteria at sprint end.

## Future features (beyond Sprint 14)

- Additional storage providers (SQL Server, PostgreSQL, cloud) behind `IStorageProvider`.
- Directory-based dynamic third-party plugin loading.
- Monaco (WebView2) rich JSON editing across the app.
- gRPC / GraphQL / WebSocket request types.
- Scheduled/monitored runs and richer alerting.
