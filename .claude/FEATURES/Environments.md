# Environments

## Overview

An **Environment** is a named set of variables (Development / QA / Staging / Production, or custom)
that a workspace switches between so requests and workflows stay data-driven and portable. Exactly
one environment is **active** per workspace at a time; switching it re-resolves environment-scoped
`{{variables}}`. Environments are workspace-scoped and hold no secrets of their own — secret values
live on secret-typed variables or in profiles (see `Profiles.md`, `Variables.md`).

## Scope / Capabilities

- CRUD environments (name + `EnvironmentKind`).
- Select the **active** environment; the selection is persisted per-workspace and drives variable
  resolution precedence (`Environment` scope sits between `Workspace` and `Workflow`).
- Deleting an environment cascades to its environment-scoped variables.

## Domain & Contracts

- Domain: `EnvironmentDefinition` (`Id`, `WorkspaceId`, `Name`, `EnvironmentKind Kind`) in
  `ApiTestingStudio.Domain.Entities`. Environment-scoped `Variable`s reference it via
  `Variable.EnvironmentId`.
- Application: `IEnvironmentService` / `EnvironmentService` — CRUD plus `GetActiveIdAsync` /
  `SetActiveAsync`. The active id is stored as a per-workspace **Settings** row
  (`active-environment-id`, `IWorkspaceSettingRepository`) — **no schema column**, no multi-row
  `IsActive` consistency problem.
- Persistence: `IEnvironmentRepository` / `EnvironmentRepository` (EF Core). `DeleteCascadeAsync`
  removes the environment and its variables in one transaction.

## UI

- The **Profiles & Environments** tool panel (Environments tab) offers New/Edit/Delete.
- The **toolbar environment switcher** (`EnvironmentSwitcherViewModel`) selects the active
  environment; selecting one persists it and re-scopes resolution. See `UI/ProfilesAndEnvironments.md`.

## Sprint

- **Sprint 10** — `EnvironmentDefinition` wiring, services/repository, active-environment selection,
  manager tab + toolbar switcher.

## Open Questions / Future

- Per-environment variable diff and bulk edit.
- Per-environment profile overrides (different creds per environment).
- Import of Postman environments.
