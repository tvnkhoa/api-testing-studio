# Import

## Overview

Import brings existing API definitions into a Workspace so users do not re-key requests by hand.
It supports the formats developers actually have on disk or behind a running service, and it can
**auto-detect** an API definition from just a base URL. Every format is handled by a dedicated
plugin behind a single contract, so adding a new source is a plugin, not a core change.

## Scope / Capabilities

Supported sources:

- **cURL** command (single request).
- **OpenAPI** document — JSON or YAML (file).
- **Swagger URL** and **OpenAPI URL** (fetched from a running service).
- **Scalar** — .NET 9 / .NET 10 Scalar API reference endpoint.
- **Postman Collection** and **Postman Environment**.

**Auto endpoint detection** — given a base URL (e.g. `https://localhost:5000`) the importer probes
common definition paths in order and imports the first that responds:

- `/openapi.json`
- `/openapi/v1.json`
- `/swagger.json`
- `/swagger/v1/swagger.json`
- `/scalar`

Import maps discovered operations to Endpoints (method, path, headers, params, body schema) and,
where applicable (Postman Environment), to Variables/Environments. Network access happens **only**
when the user explicitly triggers a URL-based import (offline-first).

OpenAPI/Swagger mapping specifics (`Import.OpenApi`): path parameters are preserved in the path
template (`/orders/{id}`); query parameters are appended as an editable template
(`?limit=&q=`), since an Endpoint has no dedicated query field; header parameters become
`DefaultHeaders`; and the `application/json` request body becomes `DefaultBody` — the media type's
`example` when present, otherwise a JSON skeleton generated from the schema (objects → properties,
arrays → one item, primitives → empty/zero/false), bounded to guard against `$ref` cycles.
Non-JSON request bodies are not synthesized. **Postman Environment → Variables/Environments is not
yet implemented** (see recovery backlog).

## Domain & Contracts

Domain records (`ApiTestingStudio.Domain`): `Service`, `Endpoint` (method, path template, headers,
query/path params, body schema), plus `Environment` / `Variable` produced by Postman Environment
imports.

Plugin contract (`ApiTestingStudio.Plugin.Abstractions`):

- `IImporter` — `string Format`, `bool CanImport(ImportSource source)` and
  `Task<ImportResult> ImportAsync(ImportSource source, CancellationToken cancellationToken)`.
- `ImportSource(Format, Content?, Uri?)` describes raw text, a file path, or a URL; `ImportResult`
  returns the `Service`/`Endpoint` records to merge into the Workspace. (Postman Environment →
  `Variable`/`Environment` mapping is a later-sprint extension; the Sprint 07 contract carries
  Services and Endpoints.)

The orchestration around the importers lives in the Application layer: `ISourceFormatDetector`
(auto-detect), `IImportOrchestrator` (fetch → detect → parse → preview → merge), `IDefinitionFetcher`
(user-triggered, offline-first URL fetch + auto-probe), and `ICatalogMerger` (transactional commit
into the catalog). OpenAPI/Swagger parsing uses `Microsoft.OpenApi` v2 (JSON built in; YAML via the
`Microsoft.OpenApi.YamlReader` add-on) — Swagger 2.0 and OpenAPI 3.0 **and 3.1**, referenced only by
the `Import.OpenApi` / `Import.Scalar` plugins.

Implementations ship as `Import.*` plugins (e.g. `Import.Curl`, `Import.OpenApi`, `Import.Postman`,
`Import.Scalar`). The auto-detection probe selects the appropriate `IImporter` by trying candidate
paths and inspecting the response.

## UI

- An **Import** dialog/wizard: pick a source type or paste a URL/cURL/JSON; preview detected
  endpoints; choose which to import and the target Service.
- Progress + result summary (imported / skipped / conflicts).
- MVVM (CommunityToolkit.Mvvm); Material Design styling.

## Sprint

- **Sprint 07** — `IImporter` contract, the `Import.*` plugins, and auto endpoint detection.

## Open Questions / Future

- Conflict/merge strategy when re-importing an updated definition (overwrite vs. diff).
- Additional formats: HAR, WSDL/SOAP, gRPC reflection, GraphQL introspection.
- Import of Postman scripts (pre-request/tests) — likely mapped to workflow nodes, deferred.
- Authentication for protected definition URLs (via Profiles).
