# Sprint 06 — API Runner

## Goal
Deliver the API Runner: build and send HTTP requests, view responses with timing, manage headers/cookies, and keep per-endpoint request history — the core interactive testing surface.

## Scope
- Request builder: method, URL, query params, headers, body (raw/JSON/form).
- Response viewer: status, headers, body (Monaco via WebView2), size, timing.
- Header + cookie management (per request; cookie jar per workspace — uncertain scope).
- Timing breakdown (DNS/connect/TTFB/total where available).
- Request history persisted per endpoint with replay.

## Requirements
- Sends via `HttpClient` with configurable timeout and cancellation.
- Body/response rendered in Monaco with syntax highlighting and formatting.
- History captures request + response snapshot and is re-runnable.
- Consumes endpoint-selection events from Service Explorer (Sprint 05).

## Architecture Impact
- Introduces an HTTP execution service abstraction reused by Workflow (S08) and Stress (S12).
- Establishes response/timing models shared across runners.
- First WebView2/Monaco integration in a feature panel.

## Projects (which solution projects change)
- Domain — Request, Response, RequestHistory, Timing models.
- Application — request execution + history services.
- Infrastructure — HttpClient-backed executor, history repository, migration.
- UI — Runner panel, Monaco editor host, timing/response views.
- Tests: Application.Tests, Infrastructure.Tests.

## Classes
- `HttpRequestModel`, `HttpResponseModel`, `RequestTiming`, `RequestHistoryEntry`.
- `RequestExecutionService`, `HttpRequestExecutor`, `RequestHistoryService`.
- `ApiRunnerViewModel`, `RequestBuilderViewModel`, `ResponseViewerViewModel`, `MonacoEditorViewModel`.
- `CookieJar` (uncertain).

## Interfaces
- `IRequestExecutor`, `IRequestExecutionService`.
- `IRequestHistoryService`, `IRequestHistoryRepository`.
- `IMonacoBridge` (JS interop wrapper).

## Database Changes
- New tables: `RequestHistory` (endpoint id, request snapshot, response snapshot, timing, timestamp).
- Extend `Endpoints` with default headers/body columns.
- Migration: `AddRequestHistory`.

## Plugin Changes
- None. (Assertions against responses arrive in Sprint 11.)

## UI Changes
- New Runner panel: request builder tabs (params/headers/body), Send button, response viewer, timing panel, history list.
- Monaco-based editors for request body and response.

## Acceptance Criteria
- Send a GET/POST and view status, headers, timed body response.
- Header/cookie edits are applied to the outgoing request.
- History records each send and can replay a prior request.
- Monaco renders and formats JSON responses.
- Cancel aborts an in-flight request.

## Out of Scope
- Assertions/tests (Sprint 11).
- Auth/profiles/environments (Sprint 10) — hardcoded values acceptable for now.
- Workflow chaining (Sprint 08).

## Risks
- WebView2/Monaco initialization and offline asset bundling.
- Accurate low-level timing from `HttpClient` (may need `SocketsHttpHandler` metrics).
- Large response bodies memory/perf.

## Future Improvements
- Response diffing between runs.
- GraphQL/gRPC/WebSocket support.
- Request code-gen (curl, C#).

## Checklist
- [x] HTTP executor + execution service + tests.
- [x] Request builder UI (params/headers/body).
- [x] Response viewer + Monaco integration.
- [x] Timing capture + display.
- [x] History persistence + replay.

## Outcome (2026-07-15)

Delivered the API Runner end-to-end across Domain, Application, Infrastructure and UI, build clean
(0 warnings, warnings-as-errors) with all tests green (Domain 3, Application 51, UI 29,
Infrastructure 36, PluginHost 9). See `DECISIONS/ADR-0009-*`, `FEATURES/Runner.md`, `UI/Runner.md`.

**What shipped**
- Domain: `HttpRequestModel`, `HttpResponseModel`, `RequestTiming`, `HttpExecutionResult`,
  `RequestHistoryEntry`, `HttpHeader`, `QueryParam` (`Entities/ApiRunner.cs`); `BodyKind` enum;
  `Endpoint` extended with `DefaultHeaders`/`DefaultBody`.
- Application: ports `IRequestExecutor`, `IRequestHistoryRepository`; services
  `RequestExecutionService`, `RequestHistoryService`; `RequestExecutionErrors`.
- Infrastructure: `HttpRequestExecutor` (single long-lived `SocketsHttpHandler`, DNS/connect timing
  via `ConnectCallback`), `RequestHistoryRepository`, `WorkspaceDbContext` mapping, migration
  `AddRequestHistory`, `SchemaVersion` → 3.
- UI: `ApiRunnerViewModel` (+ builder/response/Monaco child VMs), `ApiRunnerView` /
  `MonacoEditorView`, `IMonacoBridge`/`MonacoBridge`, offline `Assets/monaco/editor.html`, shell
  open/focus on `EndpointSelected`.

**Deviations from the plan**
- **Cookie jar dropped** — cookies are managed as plain `Cookie` request headers; a per-workspace
  cookie jar is deferred (revisit for Workflow S08). `CookieJar` class not created.
- **Single reused Runner pane** (`document.runner`) instead of a pane-per-endpoint, so AvalonDock
  layout persistence works with the existing `ContentId` scheme. Per-endpoint tabs deferred.
- **HTTP engine owns its `SocketsHttpHandler`** rather than using `IHttpClientFactory`, to keep the
  DNS/connect timing instrumentation; `Microsoft.Extensions.Http` was therefore not added.
- **Monaco bundle vendored** — `monaco-editor@0.55.1` (`min/vs`, ~16 MB) is committed under
  `Assets/monaco/vs/` so the app ships full Monaco offline. `editor.html` still self-degrades to a
  plain editor if the bundle is ever absent; JSON formatting works either way. Re-pin steps in the
  folder README.

**Verification note**
Backend verified by unit + real-SQLite integration tests. GUI end-to-end (live HTTP send, Monaco
rendering, cancel) is to be exercised by running the app on a Windows desktop session; syntax
highlighting appears once the Monaco `vs/` bundle is added.
