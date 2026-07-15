# ADR-0009 — API Runner: HTTP execution abstraction & offline Monaco host

- **Status:** Accepted
- **Date:** 2026-07-15

## Context

Sprint 06 delivers the API Runner: build/send HTTP requests, view timed responses, and keep
per-endpoint history. Two decisions have architectural reach beyond this sprint. First, the HTTP
execution engine is explicitly reused by Workflow (S08) and Stress (S12), so it must be a swappable
port, not code baked into the UI. Second, the Runner is the product's **first** Monaco/WebView2
integration, and the product is **100% offline** (`CLAUDE.md`) — no CDN, no network at load time —
so how Monaco assets are sourced and hosted must be settled deliberately. Clean Architecture applies
throughout: `UI` never references `Infrastructure`; only `Host` composes concrete adapters.

## Decision

1. **HTTP execution is an Application port with an Infrastructure adapter.** `IRequestExecutor`
   (`Application.Abstractions`) takes a fully-assembled `HttpRequestModel` and returns
   `Result<HttpExecutionResult>` (response + timing). `HttpRequestExecutor` (Infrastructure) is the
   only place `HttpClient` lives. Workflow/Stress consume the same port, and tests substitute a fake
   — no HTTP in unit tests. Feature orchestration (validate URL, execute, record history) sits in
   `RequestExecutionService`; history read/replay in `RequestHistoryService`.

2. **A single long-lived `SocketsHttpHandler`, executor-owned — not `IHttpClientFactory`.** For a
   desktop app the MS-recommended shape is one shared handler with `PooledConnectionLifetime`; it
   avoids socket exhaustion and refreshes DNS. Owning the handler also lets the executor instrument
   a `ConnectCallback` to measure DNS + connect time per new connection, flowing the values back via
   an `AsyncLocal` holder set before each send. `Total`/`TimeToFirstByte` are always measured;
   `Dns`/`Connect` are null on reused pooled connections. This avoided taking a dependency on
   `Microsoft.Extensions.Http` purely to lose the timing hook.

3. **Transport failures are typed `Result`s, not exceptions.** Cancellation → `request.cancelled`,
   `HttpClient.Timeout` → `request.timeout`, `HttpRequestException` → `request.failed`. The service
   validates the URL up front (`request.url_required` / `request.invalid_url`) and only records
   history on a completed response.

4. **History persists JSON snapshots + denormalized columns.** `RequestHistoryEntry` stores full
   `RequestSnapshot`/`ResponseSnapshot` JSON (`System.Text.Json`) for replay, plus denormalized
   method/url/status/timing for cheap list rendering — consistent with the flat, navigation-free
   entity model. Timestamp ordering is client-side (SQLite cannot `ORDER BY` a `DateTimeOffset`).
   Migration `AddRequestHistory`, schema version bumped 2 → 3.

5. **Monaco is bundled into the repo and hosted over a virtual https host — offline.** The editors
   load from `src/ApiTestingStudio.UI/Assets/monaco/` via
   `CoreWebView2.SetVirtualHostNameToFolderMapping` (no CDN). `IMonacoBridge`/`MonacoBridge` isolate
   the view code-behind from CoreWebView2; the code-behind's only job is WebView2/JS interop, which
   is an allowed view concern.

6. **Monaco is vendored; `editor.html` self-degrades as a safety net.** The `monaco-editor@0.55.1`
   `min/vs` bundle is committed under `Assets/monaco/vs/` (~16 MB) and the build copies `Assets/**`
   to output, so the app ships full Monaco offline. The loader still probes for `vs/loader.js` and
   falls back to a styled `<textarea>` if it is ever absent (re-pin/refresh documented in
   `Assets/monaco/README.md`); both paths expose the same JS API. JSON formatting is done in C#
   (`MonacoEditorViewModel.Format`) so it works in either mode.

7. **One reused Runner pane.** The Runner is a single document pane
   (`ContentId = "document.runner"`); selecting a different endpoint reloads it rather than opening a
   new tab. This keeps AvalonDock layout persistence working with the existing `ContentId` scheme;
   per-endpoint tabs (composite `ContentId`) are deferred.

## Consequences

- Workflow (S08) and Stress (S12) reuse `IRequestExecutor` unchanged; a mock transport is trivial.
- Owning the handler means the executor is `IDisposable` (disposed by DI as a singleton) and the app
  keeps one HTTP connection pool. Switching to `IHttpClientFactory` later is possible but would drop
  the DNS/connect timing hook unless re-implemented.
- The vendored Monaco bundle adds ~16 MB to the repo/output; re-pinning is a documented copy step
  with no code change. If it were ever removed the editors degrade to plain text rather than break.
- Cookies are plain headers for now; a cookie jar is deferred to when Workflow chaining needs it.
- The virtual-host CSP in `editor.html` allows `unsafe-eval`/`blob:` workers for Monaco; acceptable
  for a local, offline, first-party asset.
