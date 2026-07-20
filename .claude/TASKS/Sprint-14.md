# Sprint 14 — Packaging & Polish

## Goal
Ship the `.apistudio` package format via the `Export.ApiStudio` plugin (`IExporter`/`IWorkspaceSerializer`), plus backup/recovery and a performance/optimization pass — making workspaces portable and the app release-ready.

## Scope
- `.apistudio` package = ZIP of `manifest.json` + `database.sqlite` + `attachments/`.
- `IExporter` + `IWorkspaceSerializer` in plugins/Export.ApiStudio.
- Export (pack) and import (unpack) round-trip of a full workspace.
- Backup (automatic/manual) and recovery from backup or crash.
- Performance & optimization pass (startup, large-catalog, chart/render hot paths).

## Requirements
- Package is self-contained and offline-portable; manifest carries schema/plugin versions.
- Round-trip export->import reproduces the workspace exactly (catalog, workflows, tests, secrets*).
- Secrets note: DPAPI-bound secrets may not decrypt on another machine — manifest flags this (uncertain handling: re-prompt vs. skip).
- Backups are versioned and restore is verified.
- Measurable perf improvements against a baseline.

## Architecture Impact
- Defines `IExporter` and `IWorkspaceSerializer` contracts in Plugin.Abstractions.
- Introduces a packaging/versioning boundary and a backup service.
- Consolidates cross-cutting perf work (async, virtualization, caching).

## Projects (which solution projects change)
- Plugin.Abstractions — `IExporter`, `IWorkspaceSerializer`, package/manifest contracts.
- plugins/Export.ApiStudio — packaging implementation.
- Application — export/import orchestration, backup/recovery service.
- Infrastructure — file/ZIP handling, backup store, DB checkpoint/vacuum.
- UI — export/import dialogs, backup settings, progress.
- Tests: PluginHost.Tests, Infrastructure.Tests, Application.Tests.

## Classes
- `ApiStudioExporter` (`IExporter`), `ApiStudioWorkspaceSerializer` (`IWorkspaceSerializer`).
- `PackageManifest`, `WorkspacePackager`, `WorkspaceUnpacker`.
- `BackupService`, `RecoveryService`, `AttachmentStore`.
- `ExportDialogViewModel`, `BackupSettingsViewModel`.

## Interfaces
- `IExporter` (pack workspace to package).
- `IWorkspaceSerializer` (serialize/deserialize workspace <-> package).
- `IBackupService`, `IRecoveryService`.

## Database Changes
- No new domain tables required.
- DB maintenance ops: WAL checkpoint + `VACUUM` before packaging.
- Uncertain: manifest/version bookkeeping row if not already in WorkspaceMetadata (S02).

## Plugin Changes
- Implement Export.ApiStudio against `IExporter`/`IWorkspaceSerializer`; declare `IExporterPlugin`.

## UI Changes
- Export/Import package dialogs with progress and validation.
- Backup settings (schedule, retention) and restore flow.
- Polish pass on existing panels (spacing, icons, empty states).

## Acceptance Criteria
- Export a workspace to `.apistudio` and re-import it into a fresh install with data intact.
- Package structure = manifest.json + database.sqlite + attachments/.
- Manual and automatic backups create restorable archives; recovery works after a simulated crash.
- Perf pass shows improved startup and large-catalog responsiveness vs baseline.
- Manifest version mismatches are detected and reported on import.

## Out of Scope
- Cloud backup / sync.
- Cross-format export (Postman/OpenAPI export) — could be future plugins.
- Auto-update/installer (separate release-engineering effort).

## Risks
- Secret portability across machines (DPAPI) breaking round-trip fidelity.
- Large workspace packaging time/memory.
- Backward/forward compatibility of the package/manifest schema.

## Future Improvements
- Incremental/differential backups.
- Selective export (subset of services/workflows).
- Export to interchange formats (OpenAPI/Postman).

## Decisions (ADR-0012)
- Layering: plugin does **pure ZIP** pack/unpack (`IWorkspaceSerializer`); Application
  `IWorkspacePackageService` orchestrates; Infrastructure does SQLite maintenance + backup store.
- `IExporter` repurposed to a **format-capability** declaration (Format/DisplayName/FileExtension).
- Secret portability: **flag + re-prompt** (manifest carries a non-reversible key fingerprint).
- Attachments: **sidecar `<name>.attachments/`** folder next to the `.atsdb`.
- No schema change; `Workspace.CurrentSchemaVersion` stays at 9 (manifest bookkeeping is derived).

## Performance pass (Sprint 14)
- **Package size/IO:** `VACUUM INTO` yields a compacted, WAL-free snapshot, bounding package size and
  import time; packaging streams the DB/attachments rather than buffering whole files in memory.
- **Large-catalog rendering:** the Service Explorer tree already used recycling virtualization; this
  sprint extended UI virtualization + container recycling to the **Timeline** run list + step tree and
  the **Log Viewer** DataGrid (row + column virtualization), so those surfaces stay responsive with
  thousands of rows. Measure by scrolling a large run/log workspace before vs. after.

## Checklist
- [x] `IExporter` (capability) + `IWorkspaceSerializer` (enriched) + `PackageManifest` contracts.
- [x] Package pack/unpack (manifest + db + attachments) in `Export.ApiStudio` + round-trip test.
- [x] `IWorkspaceMaintenance` (checkpoint + `VACUUM INTO`) in Infrastructure.
- [x] `IWorkspacePackageService` export/import orchestration + secret re-prompt detection.
- [x] Backup + recovery services (versioned archives, verified restore) + tests.
- [x] Export/import + backup UI (dialogs, progress, mismatch/re-prompt reporting).
- [x] Performance pass (large-catalog virtualization; VACUUM-compacted packages).
