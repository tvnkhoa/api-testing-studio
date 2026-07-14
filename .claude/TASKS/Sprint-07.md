# Sprint 07 — Import System

## Goal
Implement the import pipeline and the `Import.*` plugins so users can bring endpoints in from cURL, OpenAPI/Swagger (file or URL), Scalar (.NET 9/10), and Postman — with source auto-detection and a guided import wizard.

## Scope
- `IImporter` implementations in Import.Curl, Import.OpenApi, Import.Scalar, Import.Postman.
- Swagger/OpenAPI import from a URL (offline-friendly: fetch-once) and from file.
- Auto-detection of source format from pasted text / file / URL.
- Import wizard: pick source -> preview parsed endpoints -> map to services -> confirm.
- Merge into the Service Explorer catalog (Sprint 05).

## Requirements
- Each importer is a discovered plugin (Sprint 03 infrastructure).
- Parsers tolerate malformed input with actionable errors.
- Preview shows what will be created/updated before commit.
- Import is transactional; partial failures roll back or are clearly reported.

## Architecture Impact
- Defines the `IImporter` contract in Plugin.Abstractions and the import orchestration in Application.
- Establishes a canonical intermediate model that importers produce and the catalog consumes.

## Projects (which solution projects change)
- Plugin.Abstractions — `IImporter`, import result/model contracts.
- plugins/Import.Curl, Import.OpenApi, Import.Scalar, Import.Postman.
- Application — import orchestration, auto-detect, catalog merge.
- UI — import wizard.
- Tests: PluginHost.Tests, Application.Tests.

## Classes
- `CurlImporter`, `OpenApiImporter`, `ScalarImporter`, `PostmanImporter`.
- `ImportOrchestrator`, `SourceFormatDetector`, `CatalogMerger`.
- `ImportWizardViewModel`, `ImportPreviewViewModel`.
- `ParsedEndpoint`, `ImportResult`, `ImportOptions`.

## Interfaces
- `IImporter` (id, supported formats, `CanImport`, `ImportAsync`).
- `ISourceFormatDetector`, `IImportOrchestrator`, `ICatalogMerger`.

## Database Changes
- None new required; writes into `Services`/`Endpoints` from Sprint 05.
- Possibly record import provenance on endpoints (uncertain — optional column/migration).

## Plugin Changes
- Implement all four Import.* plugins against `IImporter`.
- Each ships a manifest declaring the `IImporterPlugin` capability.

## UI Changes
- Import wizard dialog (source selection, paste/file/URL, preview grid, mapping, confirm).
- Entry points from Service Explorer and main menu.

## Acceptance Criteria
- Import a cURL command and see the endpoint created.
- Import an OpenAPI/Swagger file and a URL; endpoints and methods parsed correctly.
- Import a Postman collection; folders/requests mapped.
- Auto-detect chooses the correct importer for pasted content.
- Preview accurately reflects the committed result.

## Out of Scope
- Export (Sprint 14).
- Auth/environment extraction beyond basic mapping (Sprint 10).
- Continuous/watched re-import sync.

## Risks
- OpenAPI 3.x vs Swagger 2.0 variance; large specs performance.
- Scalar (.NET 9/10) format stability and parsing details (needs validation).
- Postman schema versions and auth/variable constructs.
- URL import while "100% offline" — must be explicit, user-initiated fetch.

## Future Improvements
- Re-import/diff-merge with change detection.
- gRPC/AsyncAPI importers.
- Import from HAR / browser recordings.

## Checklist
- [ ] Define `IImporter` + intermediate model.
- [ ] Implement curl / OpenAPI / Scalar / Postman importers.
- [ ] Source auto-detection.
- [ ] Import wizard UI + preview.
- [ ] Catalog merge + transactional commit + tests.
