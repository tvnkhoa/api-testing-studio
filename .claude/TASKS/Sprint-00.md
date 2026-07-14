# Sprint 00 — Architecture Validation & Technical Spike

## Goal

Validate the overall architecture before writing production code, so Foundation (Sprint 01) is
built on approved, low-risk decisions.

## Scope

- Review Clean Architecture layering and project boundaries.
- Finalize the plugin architecture and the core plugin contracts.
- Define the `.apistudio` package structure.
- Create the initial Architecture Decision Records.
- Perform a risk assessment and validate the solution dependency graph.

## Deliverables

- `ARCHITECTURE.md`, `PLUGIN_DEVELOPMENT.md`, `DATABASE_GUIDELINES.md`, `CODING_STANDARDS.md`,
  `UI_GUIDELINES.md`, `CLAUDE.md`, `ROADMAP.md`.
- ADR-0001 (WPF), ADR-0002 (Plugin architecture), ADR-0003 (SQLite workspace),
  ADR-0004 (Clean Architecture layering), ADR-0005 (Central Package Management).
- Plugin contract definitions in `Plugin.Abstractions`:
  `IPluginModule`, `IImporter`, `IExporter`, `IWorkspaceSerializer`, `IAssertion`,
  `IWorkflowNode`, `IStressRunner`, `IStorageProvider`, `IDashboardWidget`, `IToolWindow`.
- `.apistudio` = ZIP(`manifest.json` + `database.sqlite` + `attachments/`).

## Architecture Impact

Establishes the entire structure: eight `src` projects, nine plugins, four test projects, and
the inward-only dependency rule.

## Acceptance Criteria

- Architecture approved and documented.
- Plugin contracts finalized.
- Solution dependency graph validated (no inward-rule violations).
- Key risks identified with mitigations.

## Risks

- New-in-2025 package versions on .NET 10 (Nodify, LiveCharts2, AvalonDock, WebView2) — mitigate
  by isolating UI packages to `UI`/`Host` and deferring unused ones.
- Native SQLite advisory — mitigated by the 3.x pin (ADR-0003).

## Future Improvements

- Automated architecture/dependency test to enforce ADR-0004 in CI.

## Checklist

- [x] Clean Architecture reviewed
- [x] Plugin architecture finalized
- [x] Core interfaces defined
- [x] `.apistudio` structure defined
- [x] Initial ADRs created
- [x] Risk assessment done
- [x] Dependency graph validated
