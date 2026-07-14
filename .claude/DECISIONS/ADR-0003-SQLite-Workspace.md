# ADR-0003 — SQLite workspace via EF Core, behind a storage provider

- **Status:** Accepted
- **Date:** 2026-07-14

## Context

The app is offline-first. A workspace bundles services, endpoints, profiles, variables,
workflows, runs, logs, and attachment references. We need embedded, zero-configuration, portable
persistence — and the freedom to move to server/cloud stores later without rewriting business
logic.

## Decision

Use **SQLite** through **EF Core 10** as the Phase 1 store, accessed through an
**`IStorageProvider`** abstraction (`Plugin.Abstractions.Storage`). The SQLite implementation is
`SqliteStorageProvider`; the schema is owned by `WorkspaceDbContext` with EF Core migrations
(`InitialCreate`). Business code depends only on `IStorageProvider`.

The portable **`.apistudio`** package is a ZIP of `manifest.json` + `database.sqlite` +
`attachments/` (see the "Export ONLY .apistudio" requirement). Attachments are stored as files;
the database holds only references.

### Native SQLite security pin

EF Core's default `SQLitePCLRaw 2.1.11` carries advisory **GHSA-2m69-gcr7-jv3q** (NU1903). We
pin the native stack to **`SQLitePCLRaw 3.x`** via Central Package Management transitive pinning.
Verified working at runtime (migrations apply, DB opens).

## Consequences

- Zero-config, single-file, portable workspaces that work fully offline.
- Adding SQL Server / PostgreSQL / cloud later = a new `IStorageProvider` + one Host DI line, no
  business-logic change.
- EF migrations must never break existing workspaces; `Workspace.SchemaVersion` gates upgrades.
- DateTimeOffset has limited SQLite support (stored as text) — acceptable for our access
  patterns; revisit if range queries on time become hot.
