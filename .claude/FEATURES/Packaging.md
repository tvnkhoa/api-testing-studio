# Feature — Packaging, Backup & Recovery (Sprint 14)

> See `ADR-0012` for the decision record. This document is the feature-level reference.

## Overview

Make a workspace **portable** and the app **release-ready** via the `.apistudio` package format,
plus automatic/manual backups with verified restore. 100% offline; no cloud.

## The `.apistudio` package

A plain ZIP:

```
mypackage.apistudio
├── manifest.json        # PackageManifest (versions, plugin deps, secret binding)
├── database.sqlite      # checkpointed + VACUUM INTO copy of the workspace .atsdb
└── attachments/         # attachment blobs (may be empty)
```

### `manifest.json` (PackageManifest)

| Field                   | Meaning |
|-------------------------|---------|
| `formatVersion`         | Package layout schema (starts `1.0`). Major mismatch → import rejected. |
| `workspaceSchemaVersion`| `Workspace.CurrentSchemaVersion` at export. Newer-than-supported → import rejected. |
| `appVersion`            | Producing app version (informational). |
| `workspaceId` / `workspaceName` | Identity of the packaged workspace. |
| `createdUtc`            | When the package was produced. |
| `plugins[]`             | Plugin dependencies (`pluginId`, `pluginName`, `version`) from the workspace's `PackageMetadata` rows. Missing on import → non-fatal warning. |
| `secrets`               | `{ machineBound: bool, keyFingerprint: string }` — see below. |

## Round-trip contract

`Export → Import into a fresh install` reproduces the workspace exactly: catalog (services/
endpoints/folders), profiles/environments/variables, workflows, tests/assertions/results, stress
runs, run history, logs, settings, and attachments. **Secrets round-trip only on the same
machine/user** (DPAPI, ADR-0010).

## Secret portability — flag + re-prompt

Secrets are never exported beyond the ciphertext already inside `database.sqlite`. The manifest
stores a non-reversible **fingerprint** of the local master key (`SHA-256`, truncated) — never the
key. On import to a machine whose master-key fingerprint differs, the ciphertext cannot decrypt; the
import still succeeds and reports `SecretsNeedReprompt = true`. The UI then flags the affected
profiles/secret-variables so the user re-enters them. Nothing is silently blanked.

## Backup & recovery

- **Location:** `<app-data>/backups/<workspaceId>/` — outside any workspace file.
- **Format:** a timestamped `.apistudio` package (a backup is just a package).
- **Versioned:** filename carries a UTC timestamp; pruned to a **retention count** (app setting).
- **Manual:** `File → Backup Now`.
- **Automatic:** opt-in (on close / scheduled), same code path.
- **Restore:** `IRecoveryService` lists backups and restores one by unpacking to a target and
  **verifying** the restored DB opens before reporting success (covers crash-recovery).

## DB maintenance before packaging

`IWorkspaceMaintenance.CheckpointAndVacuumAsync` runs `PRAGMA wal_checkpoint(TRUNCATE)` then
`VACUUM INTO <temp>` so the package DB is compacted, WAL-free, and consistent — the live file is
never mutated.

## Components

| Layer | Type | Responsibility |
|-------|------|----------------|
| Plugin.Abstractions | `PackageManifest`, `WorkspacePackageRequest`, `WorkspacePackageContents`, `IWorkspaceSerializer`, `IExporter` | Contracts + package DTOs. |
| plugins/Export.ApiStudio | `ApiStudioPackageSerializer`, `WorkspacePackager`, `WorkspaceUnpacker`, `PackageManifestSerializer`, `AttachmentStore` | Pure ZIP pack/unpack of the format. |
| Application | `IWorkspacePackageService` / `WorkspacePackageService`, `IWorkspaceMaintenance`, `IBackupService`, `IRecoveryService`, `PackageExportResult`, `PackageImportResult`, `BackupEntry` | Orchestration: maintenance → manifest → serialize; import validate/install/open; backup/restore. |
| Infrastructure | `WorkspaceMaintenance`, `BackupService`, `RecoveryService` | SQLite checkpoint/VACUUM; app-data backup store. |
| UI | `ExportDialogViewModel`, `BackupSettingsViewModel`, shell/menu commands | Export/import dialogs, backup settings, progress, mismatch + re-prompt reporting. |

## Out of scope (future)

Cloud backup/sync; incremental/differential backups; selective export; cross-format export
(OpenAPI/Postman); auto-update/installer.
