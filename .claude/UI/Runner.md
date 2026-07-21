# Runner (UI)

> UX rationale & benchmark (Postman/Insomnia + Monaco): see `../UI_BENCHMARK.md` (Feature Mapping, Run Request journey).

## Overview

The **API Runner** is the primary **document pane** (centre of the shell), where the user builds
and sends a request and inspects the response. It is opened/focused when an endpoint is selected in
the Explorer. See the feature contract in `FEATURES/Runner.md` and the pane model in `DockLayout.md`.

## Composition

- `ApiRunnerViewModel : DocumentPanelViewModel` — `ContentId = "document.runner"`, title tracks the
  loaded endpoint. Implements `IRecipient<EndpointSelectedMessage>`; registers on the injected
  `IMessenger` in its constructor. Composes child view models and holds the history collection.
  - `RequestBuilderViewModel` — method/url/params/headers/body; `Build()` → `HttpRequestModel`;
    `LoadFromEndpoint(endpoint, baseUrl)` and `LoadFromRequest(model)` (replay). Body is a
    `MonacoEditorViewModel`.
  - `ResponseViewerViewModel` — status/size/timing header, headers grid, read-only Monaco body.
  - `MonacoEditorViewModel` — `Text`/`Language`/`IsReadOnly` + a C# `FormatCommand` (JSON
    pretty-print, independent of Monaco).
  - `KeyValueRowViewModel` — editable enabled/name/value row for headers and params.

## Views

- `Views/Runner/ApiRunnerView.xaml` — request line (method + URL + Send/Cancel), request tabs
  (Params / Headers / Body), a response section (status/size/timing + Body/Headers tabs), and a
  history list (Replay / Clear). No code-behind logic.
- `Views/Runner/MonacoEditorView.xaml` — hosts a `WebView2`. Code-behind is limited to WebView2/JS
  interop (allowed view concern): it bridges `MonacoEditorViewModel.Text` ↔ the hosted editor via
  `MonacoBridge` and degrades silently if the WebView2 runtime is unavailable.
- `DataTemplate`s mapping both view models to their views live in `Resources/PanelTemplates.xaml`.

## Navigation & messaging

Selecting an endpoint in the Explorer publishes `EndpointSelected` (`API/Events.md`). Two recipients
react: `ShellViewModel` opens-or-focuses the single shared Runner pane (checks `Documents` for
`document.runner` before adding), and `ApiRunnerViewModel` loads the endpoint's method/URL/defaults
and its history. **One reused pane** across endpoints (not a tab per endpoint) — keeps layout
persistence working with the existing `ContentId` scheme.

## Monaco / WebView2 host

The editors load Monaco from bundled assets over a virtual https host
(`CoreWebView2.SetVirtualHostNameToFolderMapping`), fully offline. `Assets/monaco/editor.html` is
committed and **self-degrades** to a plain `<textarea>` when the Monaco `vs/` bundle is absent, so
the Runner is usable before the bundle is dropped in (see `Assets/monaco/README.md` and
`DECISIONS/ADR-0009-*`). `IMonacoBridge` isolates the code-behind from CoreWebView2 details.

## Sprint

- **Sprint 06** — Runner document pane, request builder, response viewer, timing, history + replay,
  first Monaco/WebView2 host.

## Open Questions / Future

- Multiple Runner tabs (per-endpoint panes) with a composite `ContentId` for layout persistence.
- Response diffing, code-gen, and richer body types (form-data files, GraphQL).
- Applying theme (light/dark) to the Monaco editor in lockstep with the shell theme toggle.
