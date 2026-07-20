# Logging

## Overview

Logging covers two related concerns: **execution logging** (a full, replayable record of every run)
and **application logging** (diagnostics for the app itself). Every execution — a single request, a
workflow, or a stress test — is persisted as a structured tree so the user can inspect exactly what
happened and replay it. All data stays in the local SQLite store; nothing is sent anywhere.

## Scope / Capabilities

Execution log — every run stores a tree:

```
Run → RunSteps → Requests → Responses → Assertions → Logs
```

- **Replay** — re-execute a stored run (or step) exactly as recorded.
- **History** — every request the user sends is stored; from history a request can be **Replayed**,
  **Duplicated**, or **Saved as Endpoint**.
- Full request/response detail: headers, body, status, latency, timestamps, assertion outcomes.
- Secrets are never written to logs (see `Profiles.md`).

Application log:

- **Serilog** for structured app diagnostics (startup, plugin load, errors), written through
  `ILogger<T>`. No `Console.WriteLine`; no secrets logged.

## Domain & Contracts

Domain records (`ApiTestingStudio.Domain`), the execution tree:

- `Run` — id, workspace id, source (request / workflow / stress), start/end, overall status.
- `RunStep` — one node/step within a run; parent run id, order, status.
- `Request` — method, url, headers, body, timestamp, profile/environment context.
- `Response` — status code, headers, body, latency.
- `AssertionResult` — assertion name, expected/actual, pass/fail.
- `LogEntry` — timestamped message attached to a run/step.
- `HistoryEntry` — a stored request for the History view (supports Replay / Duplicate / Save as
  Endpoint).

Persistence flows through `IStorageProvider` (SQLite via EF Core); the UI never touches EF Core
directly. Replay re-drives the recorded `Request`s through the normal execution path.

## UI

- **Run detail** view: an expandable tree (AvalonDock pane) mirroring Run → RunSteps → Requests →
  Responses → Assertions → Logs, with a Replay action at run and step level.
- **History** view: chronological request list with Replay / Duplicate / Save as Endpoint.
- Request/response bodies shown in the **Monaco** editor (WebView2). MVVM (CommunityToolkit.Mvvm);
  Material Design.

## Sprint

- **Sprint 13** — execution log tree, History, Replay, and Serilog app logging wiring.

### Delivered (Sprint 13)

Execution logging is realised as a **unified run tree**: `Run` (source-discriminated:
request / workflow / stress) + `RunStep` (nested via `ParentStepId`, with request/response JSON
snapshots), written through `IRunStore` by `IRunRecorder` and read by the Dashboard, Timeline, and
Replay. `IRunReplayService` re-drives a recorded run (request snapshots re-execute; workflows re-run;
stress is not replayable). Application logging is persisted to a per-workspace **`LogEvents`** table
(entity `LogEventRecord`, distinct from the execution `LogEntry`) via a buffered Serilog sink
(`WorkspaceDbLogSink`: bounded pre-open ring, batched flush to the open workspace); the Log Viewer
filters it by level / source / text. See **ADR-0011**.

## Open Questions / Future

- Retention / purge policy and DB size management for large histories.
- Export a run tree (or history) to a shareable report / `.apistudio` package.
- Diff two runs of the same workflow.
- Redaction rules for logging sensitive response fields.
