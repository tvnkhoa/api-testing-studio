# ADR-0012 — `.apistudio` package format, backup/recovery, and secret portability

- **Status:** Accepted
- **Date:** 2026-07-20

## Context

Sprint 14 (Packaging & Polish) makes a workspace portable and the app release-ready. Until now a
workspace was a single self-contained SQLite file (`.atsdb`, ADR-0003) opened in place, with
attachments referenced by metadata but never actually written, and secrets encrypted at rest with an
AES-256-GCM key wrapped by **Windows DPAPI, bound to the current user/machine** (ADR-0010). There
was no export/import format, no backup story, and no defined attachment storage location.

We need: a portable package that round-trips a full workspace offline; automatic and manual backups
with verified restore; and a defined answer to the fact that DPAPI-bound secrets cannot be decrypted
on another machine.

## Decision

### 1. `.apistudio` package = ZIP(`manifest.json` + `database.sqlite` + `attachments/`)

The package is a plain ZIP (`System.IO.Compression`, no external dependency) containing:

- `manifest.json` — the `PackageManifest` (below).
- `database.sqlite` — a **checkpointed + `VACUUM INTO`** copy of the workspace `.atsdb`. The live
  file is never touched; export operates on a clean, WAL-free copy so the package is a consistent
  point-in-time snapshot.
- `attachments/` — the workspace's attachment blobs (may be empty).

The format is owned by the **`Export.ApiStudio` plugin**, the only supported export target
(cross-format export — Postman/OpenAPI — is explicitly future work).

### 2. Layering: pure plugin, Application orchestration, Infrastructure maintenance

- **Plugin (`Export.ApiStudio`)** implements `IWorkspaceSerializer` as **pure, dependency-free byte
  I/O**: `SaveAsync(WorkspacePackageRequest)` zips explicit inputs `{ dbPath, attachmentsDir,
  manifest }`; `LoadAsync(packagePath, stagingDir)` extracts to `WorkspacePackageContents
  { manifest, dbPath, attachmentsDir }`. It performs no DB work and knows nothing about the open
  session — this keeps it testable and offline. It also implements `IExporter` purely as a **format
  capability declaration** (`Format`/`DisplayName`/`FileExtension`) so the orchestrator can select
  among future exporters; the redundant `ExportAsync`/`ExportRequest`/`ExportResult` placeholder from
  the scaffold is retired.
- **Application (`IWorkspacePackageService`)** orchestrates: on **export** it resolves the source
  path from `IWorkspaceSession`, runs `IWorkspaceMaintenance.CheckpointAndVacuumAsync` into a temp
  copy, builds the `PackageManifest`, calls the serializer, and returns `PackageExportResult`. On
  **import** it calls `LoadAsync`, validates the manifest, installs `database.sqlite` +
  `attachments/` at the chosen location, opens via `IWorkspaceService`, and returns
  `PackageImportResult` (carrying `SecretsNeedReprompt`).
- **Infrastructure** implements `IWorkspaceMaintenance` against SQLite
  (`PRAGMA wal_checkpoint(TRUNCATE)` then `VACUUM INTO` a target path), plus the backup store.

### 3. `PackageManifest` carries version + dependency + secret-binding bookkeeping

`manifest.json` records: package `FormatVersion` (the ZIP-layout schema, starts at `1.0`),
`WorkspaceSchemaVersion` (`Workspace.CurrentSchemaVersion` at export), producing `AppVersion`,
`WorkspaceId`/`WorkspaceName`, `CreatedUtc`, the plugin dependency list (from the workspace's own
`PackageMetadata` rows, S02), and a `SecretBinding { MachineBound, KeyFingerprint }`. No new domain
table is required — manifest bookkeeping is derived at export time.

On import the manifest is validated before install:

- `WorkspaceSchemaVersion` is checked with `SchemaVersionValidator` — **a workspace written by a
  newer schema is rejected** (reported, not silently opened). Older schemas open and migrate as
  usual (EF migrations run on open).
- `FormatVersion` major mismatch is rejected via `VersionCompatibility`.
- Missing plugin dependencies are reported as a non-fatal warning (the workspace still opens).

### 4. Secret portability: **flag + re-prompt** (not silent, not exported)

Secrets are **never** written to the package in re-usable form beyond the ciphertext already in
`database.sqlite`. The manifest records a **non-reversible fingerprint** of the local master key
(`SHA-256(masterKey)`, truncated) — never the key itself. On import, if the local key's fingerprint
differs from the manifest's, secret ciphertext will not decrypt on this machine; the import succeeds
and returns `SecretsNeedReprompt = true`, and the UI flags affected profiles/secret-variables for
the user to **re-enter**. We deliberately reject: (a) silently blanking secrets (data-loss surprise),
and (b) re-encrypting secrets under a package passphrase (adds a crypto/passphrase path — future
work). This preserves the offline, machine-bound security posture of ADR-0010 while making the
non-portability explicit and recoverable.

### 5. Backups: versioned archives under app-data, verified restore

`IBackupService` writes a **timestamped `.apistudio` package** into a per-workspace backup folder
under the app-data directory (outside any workspace file). Backups are **versioned** (filename
carries a UTC timestamp) and pruned by a **retention count** from app settings. A manual command and
an automatic-on-close/opt-in schedule both call the same path. `IRecoveryService` lists backups and
restores one by unpacking it to a target location and **verifying** the restored DB opens
(`IStorageProvider.OpenAsync` succeeds) before reporting success — covering the "recovery after a
simulated crash" acceptance criterion. Cloud/differential backup is out of scope.

### 6. Attachments live in a sidecar folder next to the `.atsdb`

Per the `Attachment` entity contract, blobs live under a workspace-scoped
`<workspaceName>.attachments/` folder beside the `.atsdb`, addressed by `Attachment.RelativePath`.
`AttachmentStore` (in the plugin) resolves that folder for pack/unpack; the DB stores only metadata.
This keeps the DB small and makes `attachments/` a direct ZIP mapping.

## Consequences

- Export/import round-trips catalog, workflows, tests, runs, logs and attachments exactly; secrets
  round-trip **only on the same machine/user**, and are clearly flagged for re-entry elsewhere.
- The package is a superset of a backup, so backup/restore and export/import share one code path.
- Adding a future format (e.g. Postman export) means a new plugin implementing `IExporter` +
  `IWorkspaceSerializer`; the orchestrator and UI are unchanged.
- `VACUUM INTO` gives a compacted, WAL-free snapshot for free, which also bounds package size.
- No schema change this sprint; `Workspace.CurrentSchemaVersion` stays at 9.
