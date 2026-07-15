# API Runner

## Overview

The **API Runner** is the core interactive testing surface: build an HTTP request, send it, and
inspect the response with timing — entirely offline against whatever target the user points it at.
It introduces the reusable **HTTP execution abstraction** that Workflow (Sprint 08) and Stress
(Sprint 12) build on, and the first **Monaco/WebView2** editor host. Everything belongs to the open
workspace; per-endpoint request history is persisted and re-runnable.

## Scope / Capabilities

- **Request builder** — method (`HttpVerb`), URL, query params, headers, and body (raw / JSON /
  form via `BodyKind`). Rows carry an `Enabled` flag so they can be excluded without deletion.
- **Response viewer** — status + reason, headers, body (Monaco, read-only), size, and timing.
- **Timing breakdown** — total and time-to-first-byte always; DNS and connect when a new connection
  is established (measured via `SocketsHttpHandler.ConnectCallback`; null on reused pooled
  connections).
- **History + replay** — each successful send is recorded per endpoint; selecting an entry rebuilds
  the request from its stored snapshot and re-sends it. History can be cleared.
- **Cancellation** — Send is an async command; its generated `SendCancelCommand` aborts an
  in-flight request via the `CancellationToken`.
- **Cookies** — managed as ordinary `Cookie` request headers in the builder. A dedicated cookie jar
  is deferred (revisited when Workflow chaining needs it).

## Domain & Contracts

Domain records (`ApiTestingStudio.Domain.Entities`, `ApiRunner.cs`):

- `HttpRequestModel` — method, url, query params, headers, `BodyKind`, body (runtime/DTO; not
  persisted directly).
- `HttpResponseModel` — status, reason, headers, body, content length.
- `RequestTiming` — `Total` + nullable `Dns` / `Connect` / `TimeToFirstByte`.
- `HttpExecutionResult` — response + timing, returned by the execution port.
- `RequestHistoryEntry` — persisted send: `EndpointId`, denormalized method/url/status/timing, JSON
  request/response snapshots, timestamp.
- `HttpHeader`, `QueryParam` — small name/value/enabled records.

Ports (`ApiTestingStudio.Application`):

- `IRequestExecutor` (`Abstractions`) — `ExecuteAsync(HttpRequestModel, ct) → Result<HttpExecutionResult>`.
  Concrete `HttpRequestExecutor` (Infrastructure) uses a single long-lived `SocketsHttpHandler`;
  transport failures (invalid URL handled upstream, timeout, cancellation, network) become typed
  `Result` failures. **Reused by S08/S12.**
- `IRequestHistoryRepository` (`Abstractions`) — per-endpoint history persistence
  (`RequestHistoryRepository`, Infrastructure).
- `IRequestExecutionService` / `RequestExecutionService` (`ApiRunner`) — guards workspace scope,
  validates the URL, executes, and records history on a completed response.
- `IRequestHistoryService` / `RequestHistoryService` (`ApiRunner`) — list, replay
  (deserialize snapshot), and clear history.
- Typed errors in `RequestExecutionErrors` (`request.no_workspace`, `request.url_required`,
  `request.invalid_url`, `request.timeout`, `request.cancelled`, `request.failed`, …).

Persistence: `RequestHistory` table + `Endpoints.DefaultHeaders`/`DefaultBody` columns, migration
`AddRequestHistory`, schema version 3 (see `DATABASE_GUIDELINES.md`).

## UI

- MVVM (CommunityToolkit.Mvvm). `ApiRunnerViewModel : DocumentPanelViewModel`,
  `IRecipient<EndpointSelectedMessage>`; composes `RequestBuilderViewModel`,
  `ResponseViewerViewModel`, and two `MonacoEditorViewModel`s. See `UI/Runner.md`.
- Body/response editors are hosted by Monaco in a WebView2 via `IMonacoBridge`, loaded fully
  offline; the host degrades to a plain editor until the Monaco bundle is added
  (see `DECISIONS/ADR-0009-*`).

## Sprint

- **Sprint 06** — request builder, response viewer, timing, history + replay, first Monaco host.
- Depends on Sprint 05 (Service Explorer / `EndpointSelected`) and Sprint 04 (shell + docking).

## Open Questions / Future

- Per-workspace cookie jar with automatic capture/attach.
- Response diffing between runs; request code-gen (curl, C#).
- GraphQL / gRPC / WebSocket request types.
- Auth / profiles / environments substitution (Sprint 10) — values are literal for now.
- Assertions against responses (Sprint 11).
