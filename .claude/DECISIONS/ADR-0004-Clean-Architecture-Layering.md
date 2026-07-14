# ADR-0004 — Clean Architecture layering

- **Status:** Accepted
- **Date:** 2026-07-14

## Context

A long-lived product needs enforceable boundaries so frameworks (WPF, EF Core) and plugins can
change without rippling through business logic.

## Decision

Adopt **Clean Architecture** with inward-only dependencies across eight projects:

`Domain` ← `Shared` / `Plugin.Abstractions` ← `Application` ← `Core` ← `Infrastructure`, with
`UI` depending on Application/Core (not Infrastructure), and `Host` as the composition root that
wires everything. The full dependency table is in `ARCHITECTURE.md` and is the authoritative
rule set.

Key invariants:
- `Core` never references a concrete plugin (only `Plugin.Abstractions`).
- `UI` never references `Infrastructure`.
- Only `Host` composes `Infrastructure` + plugins.

## Consequences

- Domain/Application stay free of framework and UI concerns → highly testable and portable.
- The UI framework or storage provider can be swapped with limited blast radius.
- Enforced by convention + code review (`CODING_STANDARDS.md` checklist); violations are build/PR
  blockers. Consider adding an automated architecture test in a later sprint.
- More projects and indirection than a monolith — accepted for maintainability at scale.
