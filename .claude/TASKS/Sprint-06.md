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
- [ ] HTTP executor + execution service + tests.
- [ ] Request builder UI (params/headers/body).
- [ ] Response viewer + Monaco integration.
- [ ] Timing capture + display.
- [ ] History persistence + replay.
