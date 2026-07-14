# Sprint 02 — Workspace Storage

## Goal
Deliver a robust, offline-first workspace persistence layer on SQLite/EF Core, covering the full workspace lifecycle (create, open, close, recent), workspace metadata, and package/plugin metadata tracking.

## Scope
- Harden the scaffolded `WorkspaceDbContext`, `SqliteStorageProvider`, and `InitialCreate` migration in Infrastructure.
- Workspace lifecycle service: create / open / close / delete a workspace file.
- Workspace metadata (name, description, schema version, timestamps, id).
- Recent workspaces list (MRU) persisted outside the workspace DB (app settings store).
- Package/plugin metadata table to record which plugins a workspace depends on.

## Requirements
- Each workspace is a single self-contained SQLite file; no shared/global DB.
- EF Core migrations run automatically on open; schema version stored and validated.
- Graceful handling of missing/locked/corrupt DB files with clear errors.
- Recent workspaces capped (e.g. 10) with pinning support (uncertain — may defer pinning).
- All operations async and cancellation-aware.

## Architecture Impact
- Establishes the persistence contract consumed by all later sprints.
- Repository + Unit-of-Work pattern over `WorkspaceDbContext`.
- `IStorageProvider` abstraction remains the boundary between Application and Infrastructure.

## Projects (which solution projects change)
- Infrastructure — DbContext, migrations, repositories, storage provider.
- Application — workspace lifecycle service, DTOs, interfaces.
- Domain — Workspace + metadata entities (if not already present).
- Shared — result/error types, constants.
- Tests: Infrastructure.Tests, Application.Tests.

## Classes
- `WorkspaceDbContext` (harden), `SqliteStorageProvider` (harden).
- `WorkspaceService`, `RecentWorkspacesService`.
- `WorkspaceRepository`, `PackageMetadataRepository`.
- `WorkspaceMetadata`, `PackageMetadata`, `RecentWorkspaceEntry` entities/DTOs.
- `SchemaVersionValidator`.

## Interfaces
- `IStorageProvider` (existing) — extend with lifecycle methods.
- `IWorkspaceService`, `IRecentWorkspacesService`.
- `IWorkspaceRepository`, `IPackageMetadataRepository`.
- `IUnitOfWork` (uncertain — may fold into repositories).

## Database Changes
- Tables: `WorkspaceMetadata`, `PackageMetadata` (schema version, plugin id, version, installed-at).
- Extend/confirm `InitialCreate` migration; add `AddPackageMetadata` migration if separated.
- Schema-version row for forward-compat checks.

## Plugin Changes
- None. (Plugin metadata is *recorded* here, but plugin infrastructure is Sprint 03.)

## UI Changes
- None functional this sprint. (Recent-workspaces UI wiring lands in Sprint 04 shell.)

## Acceptance Criteria
- Create a new workspace file, close, and re-open it with metadata intact.
- Migrations apply cleanly to a fresh DB and are idempotent on re-open.
- Recent list updates on open and survives app restart.
- Corrupt/locked DB surfaces a typed error, not an unhandled exception.
- Package metadata rows can be read/written via repository.

## Out of Scope
- Multi-workspace concurrent editing.
- Cloud sync / remote storage.
- Backup & recovery (Sprint 14).

## Risks
- EF Core + SQLite file-locking on Windows during close/reopen.
- Schema migration strategy for future breaking changes (needs a versioning policy).
- MRU store location/format may need revisiting once Shell settings exist.

## Future Improvements
- Recent-workspaces **pinning** (deferred this sprint): pin entries so they never age out.
- Workspace templates and quick-start samples.
- Encrypted workspace files.
- WAL-mode tuning and vacuum-on-close.

## Checklist
- [x] Harden DbContext + migration and verify InitialCreate.
- [x] Implement workspace lifecycle service + tests.
- [x] Implement recent-workspaces MRU + persistence.
- [x] Add package metadata table + repository.
- [x] Error handling for locked/corrupt/missing DB.
- [x] Infrastructure + Application unit tests green.

## Implementation notes (delivered)
- **Contract:** `IStorageProvider` extended into a location-based lifecycle contract
  (`IsOpen`, `CreateAsync`, `OpenAsync`, `CloseAsync`, `DeleteAsync`, `GetWorkspaceAsync`,
  `SaveWorkspaceAsync`) returning `Result`. A `location` is an opaque provider-specific locator
  (a file path for SQLite), so the abstraction stays storage-agnostic.
- **One file per workspace, no global DB:** the connection string is chosen at runtime. There is
  no fixed `AddDbContext`; `WorkspaceContextFactory` builds a short-lived `WorkspaceDbContext` per
  unit of work from the `WorkspaceSession`'s current connection string. `AddInfrastructure` now
  takes the **app data directory** instead of a connection string.
- **Session:** `WorkspaceSession` (singleton) holds the open workspace; the rest of the app reads
  it through the read-only `IWorkspaceSession` port. Exactly one workspace is open at a time;
  create/open auto-close the current one first.
- **Startup:** the Host no longer creates a global `workspace.db`; it starts with no workspace
  open. Migrations run on create/open. Open/recent UI is Sprint 04.
- **Errors:** `WorkspaceErrors` catalog; `SqliteException` mapped to `Locked`/`Corrupt`;
  missing file → `NotFound`; newer file schema → `SchemaTooNew` via `SchemaVersionValidator`.
- **File handles:** `SqliteConnection.ClearAllPools()` on close/delete releases the OS handle so
  Windows reopen/delete works.
- **MRU:** `RecentWorkspacesService` persists JSON at
  `%LocalAppData%/ApiTestingStudio/recent-workspaces.json`, capped at 10, most-recent-first,
  deduped by location. **Pinning deferred** (see Future Improvements).
- **Package metadata:** `PackageMetadata` entity + `Packages` table (unique index on `PluginId`)
  + `AddPackageMetadata` migration; `IPackageMetadataRepository` upsert/read/remove.
