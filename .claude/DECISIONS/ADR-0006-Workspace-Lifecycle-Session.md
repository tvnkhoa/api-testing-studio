# ADR-0006 — Workspace lifecycle & runtime-selected storage session

- **Status:** Accepted
- **Date:** 2026-07-14

## Context

ADR-0003 established SQLite-per-workspace behind `IStorageProvider`, but the Sprint 01 scaffold
registered a single `WorkspaceDbContext` with a **fixed** connection string and migrated one
global `workspace.db` at startup. Sprint 02 requires the real model: each workspace is a
self-contained file the user **creates/opens/closes/deletes at runtime**, with **no shared/global
database**. A fixed connection string cannot express "open the file the user just picked."

## Decision

- **Extend `IStorageProvider` into a location-based lifecycle contract**: `IsOpen`, `CreateAsync`,
  `OpenAsync`, `CloseAsync`, `DeleteAsync`, plus `GetWorkspaceAsync` / `SaveWorkspaceAsync` for the
  open workspace. All return `Result` for recoverable failures. A `location` is an **opaque
  provider-specific locator** (a file path for SQLite; a connection string / database name for a
  future server provider), so the contract stays storage-agnostic.
- **No fixed `AddDbContext`.** A singleton `WorkspaceSession` holds the open workspace's connection
  string and metadata; `WorkspaceContextFactory` builds a short-lived `WorkspaceDbContext` per unit
  of work and disposes it immediately, letting connection pooling own the OS file handle.
- **Exactly one workspace open at a time.** `IWorkspaceService` (Application) orchestrates the
  lifecycle and auto-closes the current workspace before create/open. The rest of the app observes
  the open workspace through the read-only `IWorkspaceSession` port.
- **Startup opens nothing.** The Host calls `AddInfrastructure(appDataDirectory)` (not a connection
  string) and does not migrate a global DB. Migrations run on create/open. The MRU list lives
  outside any workspace DB as JSON under the app data directory.
- **Typed failures.** `WorkspaceErrors` catalogs lifecycle errors; `SqliteException` codes map to
  `Locked`/`Corrupt`; a missing file maps to `NotFound`; `SchemaVersionValidator` rejects files
  written by a newer schema (`Workspace.CurrentSchemaVersion`) as `SchemaTooNew`.

## Consequences

- Business logic still depends only on `IStorageProvider` / `IWorkspaceSession`; a server/cloud
  provider slots in without touching use cases.
- The provider must release file handles on close/delete (`SqliteConnection.ClearAllPools()`) so
  Windows allows reopen/delete — covered by tests.
- Per-unit-of-work contexts (no long-lived `DbContext`) keep the connection model simple at a small
  per-operation setup cost; acceptable for a single-user desktop app.
- Multi-workspace concurrent editing is explicitly **out of scope** (single open session). Opening
  a newer-schema workspace fails safely rather than corrupting data.
- The design-time `WorkspaceDbContextFactory` (for `dotnet ef`) is unchanged and independent of the
  runtime session.
