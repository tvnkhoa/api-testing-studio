# Deliverable 3 — Executable Scenario Catalog

> Step 2 — Executable Scenario Validation. Lightweight scenarios representing real user journeys,
> each validated by **code trace** (see `00-README.md` for method). These are *validation
> scenarios*, not a scripting language — they define the behavior the future Executable Scenarios
> MVP (Deliverable 13) will automate.

**Screenshots:** not captured in this pass (no live GUI automation); each scenario is validated by
tracing the implementation. Status legend: ✅ Works · 🟡 Partially works · 🔴 Broken · ⬜ Missing.

**Scenario summary**

| # | Scenario | Status | Worst severity |
|---|---|---|---|
| S-01 | First Launch | 🟡 | S2 |
| S-02 | Create Workspace | 🟡 | S2 |
| S-03 | Open Workspace | ✅ | S3 |
| S-04 | Import OpenAPI | ✅ | — |
| S-05 | Import Scalar | 🟡 | S3 |
| S-06 | Import Postman | 🟡 | S3 |
| S-07 | Browse Services | ✅ | S3 |
| S-08 | Browse Endpoints | ✅ | — |
| S-09 | Execute Request | 🟡 | S2 |
| S-10 | Inspect Response | 🟡 | S2 |
| S-11 | Save History | ✅ | S3 |
| S-12 | Create Environment / Profile | ✅ | S3 |
| S-13 | Switch Environment | 🟡 | S2 |
| S-14 | Switch Profile ("Run As") | 🔴 | S1 |
| S-15 | Create Workflow | ✅ | S3 |
| S-16 | Execute Workflow | 🟡 | S1 |
| S-17 | Stress Test | 🟡 | S2 |
| S-18 | Dashboard | 🟡 | S2 |
| S-19 | Timeline (drill + replay) | ✅ | S3 |
| S-20 | Logs | ✅ | — |
| S-21 | Settings | ⬜ | S2 |
| S-22 | Export Workspace | ✅ | S3 |
| S-23 | Reopen Exported Workspace | ✅ | S3 |
| S-24 | Create & Run Test Case | 🟡 | S2 |
| S-25 | Attachments | ⬜ | S3 |
| S-26 | Navigate Between Areas | 🟡 | S2 |
| S-27 | Workflow Node Data-Mapping | ⬜ | S2 |

---

## S-01 · First Launch 🟡
- **Goal:** Understand the product and know what to do first.
- **Prerequisites:** Fresh install, no prior workspace.
- **User actions:** Launch the app.
- **Expected:** A welcoming first-run screen that explains the product and offers a clear next step.
- **Observed:** App opens with no workspace; center shows a **placeholder Welcome doc** with
  Sprint-04 copy ("Feature panels dock into this shell in the sprints ahead") — `WelcomeDocumentViewModel.cs:21-23`.
  No onboarding, no first-run detection, no sample.
- **Pain points:** Stale primary screen; zero product explanation; only "use the File menu."
- **UX notes:** The empty-state pattern the docs prescribe (Bruno/Docker/GitHub Desktop) is absent
  on the most important screen.
- **Recommendation (S2):** Real state-aware Welcome with product blurb + 3 CTAs.

## S-02 · Create Workspace 🟡
- **Goal:** Get a usable workspace in ≤2 inputs.
- **User actions:** `Ctrl+N` → choose file location.
- **Expected:** Name/location/description dialog; empty workspace seeded with default environment +
  variable scopes; Welcome shows Import / Add Service / Open sample CTAs.
- **Observed:** Bare `SaveFileDialog`; **name = filename, description = null** (`ShellViewModel.cs:221-222`).
  **Nothing seeded** (`WorkspaceService.CreateAsync:26-62`). Welcome doc never changes; **no CTAs**.
- **Pain points:** Lands on an empty Explorer with nothing to click; feedback is a transient status line.
- **Recommendation (S2):** Create dialog with name/description; seed default env + scopes; live CTAs.

## S-03 · Open Workspace ✅
- **Goal:** Reopen an existing `.atsdb`.
- **User actions:** File → Open (`Ctrl+O`) or File → Open Recent.
- **Expected:** Workspace opens; theme + layout restored; last workspace remembered next launch.
- **Observed:** Open + Recent both work (`ShellViewModel.cs:227-236`, `RecentWorkspacesMenuViewModel`).
  Theme restored before window shows (`ThemeManager.cs:26-30`); dock layout restored on load
  (`DockManagerService`). **Last workspace is NOT auto-reopened** — no restore-last-session logic.
- **Pain points (S3):** Every launch starts workspace-less even for a daily user; layout/theme are
  global, not per-workspace.
- **Recommendation:** Remember & optionally auto-reopen the last workspace; scope layout per workspace.

## S-04 · Import OpenAPI ✅
- **Goal:** Populate the catalog from an OpenAPI/Swagger file.
- **User actions:** Import → pick file → preview → Merge.
- **Expected:** Services/endpoints appear in the Explorer.
- **Observed:** Fully wired (`ImportWizardViewModel` → `ImportOrchestrator` → `OpenApiImporter` →
  `OpenApiEndpointMapper`); Explorer reloads with "Import complete." The mapper now maps **method,
  path, header params, query params (as editable URL template), and a default JSON body** from
  example or schema skeleton — **the `ROADMAP.md` claim "maps method/path/headers only" is stale.**
- **Pain points:** Query params are baked into the URL string, not the Runner's Params grid (see S-09).
- **Recommendation:** Update the roadmap note; surface query params in the grid.

## S-05 · Import Scalar 🟡
- **Goal:** Import from a Scalar-documented API by base URL.
- **Observed:** A Scalar base URL **does** import — but through the **OpenAPI** path, not the Scalar
  plugin. `DefinitionFetcher` probes `/openapi.json`…`/scalar`; the `/scalar` HTML is **rejected** by
  `LooksLikeDefinition` (`DefinitionFetcher.cs:120-123`), and the resolved URI loses the `/scalar`
  marker, so `ScalarImporter.CanImport` (keys on `uri.Contains("/scalar")`, `ScalarImportPlugin.cs:37`)
  **is never selected.** The Scalar importer is effectively **dead code**.
- **Pain points (S3):** A shipped, documented plugin never runs; "Scalar import" works only by accident.
- **Recommendation:** Either route detection to the Scalar importer or fold it into OpenAPI and drop
  the separate plugin claim.

## S-06 · Import Postman 🟡
- **Observed:** Collection import **works** (one Service + one Endpoint per request, folder
  recursion, headers honoring `disabled`) — `PostmanCollectionParser`. But: **environment import
  does not exist** (module advertises "collections and environments" — `PostmanImportPlugin.cs:7`);
  **only `raw` bodies** are imported (formdata/urlencoded silently dropped, `:151-161`); `{{var}}`
  templates are kept as literal text with no mapping to workspace variables.
- **Pain points (S3):** Over-promises; silent body loss; imported templates don't connect to Variables.
- **Recommendation:** Implement environment import or correct the claim; warn on dropped body modes.

## S-07 · Browse Services ✅ / ## S-08 · Browse Endpoints ✅
- **Observed:** `ServiceExplorerViewModel` is complete — Service→Folder→Endpoint tree, live search
  with auto-expand, context menu + toolbar, full CRUD, duplicate, Move Up/Down, per-workspace
  expand/selection persistence, virtualization.
- **Pain points (S3):** **No inline/F2 rename** (rename goes through a dialog); **drag-drop is
  outbound only** (drags an endpoint to the Workflow Designer — `ServiceExplorerView.xaml.cs:30-56`);
  there is **no in-tree drop target**, so reorder/reparent is Move Up/Down only.
- **Recommendation:** Add F2 inline rename and in-tree drag reordering (both are `UI_BENCHMARK` rules).

## S-09 · Execute Request 🟡
- **Goal:** Send a request and get a response.
- **Observed:** Endpoint selection → `EndpointSelectedMessage` → Runner opens/focuses → send.
  Variable resolution **works** (`RequestExecutionService.ResolveAndAuthorizeAsync:87-101`); history +
  unified run recorded. **But three breaks:**
  - 🔴 **Profile auth never applied from the Runner.** The service accepts a `profileId` and applies
    `IAuthApplicator`, but `ApiRunnerViewModel.SendAsync` calls it **without a profile** (`:83-85`) and
    `RequestBuilderViewModel` has **no auth tab / profile selector**. (See S-14.)
  - 🔴 **Cancel button is dead.** The view binds `SendCancelCommand` (`ApiRunnerView.xaml:41`) but
    `[RelayCommand]` on `SendAsync` doesn't set `IncludeCancelCommand`, so **no such command is
    generated** (`ApiRunnerViewModel.cs:79`). Requests aren't cancellable despite the button.
  - 🟡 **No `Ctrl+Enter`** to send (button-only); query params don't populate the Params grid (folded
    into URL, `RequestBuilderViewModel.cs:70-71`).
- **Recommendation (S2):** Add auth tab + profile picker; fix Cancel; add `Ctrl+Enter`; hydrate grid.

## S-10 · Inspect Response 🟡
- **Observed:** Status, success flag, size, timing breakdown (dns/connect/ttfb), headers all render
  as plain WPF — robust offline (`ResponseViewerViewModel.cs:36-55`). **The body renders only via
  Monaco in a WebView2.** Assets are genuinely local (no CDN). **Risk (S2):** if the WebView2
  Evergreen Runtime is absent, `OnLoaded` swallows the error and the editor is **blank**
  (`MonacoEditorView.xaml.cs:49-53`) with **no plain-WPF TextBox fallback** — bodies become invisible
  while status/headers/timing still show. Undercuts the "100% offline" guarantee.
- **Recommendation:** Add a plain-`TextBox` fallback at the WPF layer when WebView2 is unavailable.

## S-11 · Save History ✅
- **Observed:** Every send writes a per-endpoint `RequestHistoryEntry` with request/response
  snapshots + timings; Runner lists it; **Replay** and **Clear** work.
- **Pain points (S3):** No "duplicate" / "save as endpoint" from history; **two parallel history
  stores** (`RequestHistoryEntry` *and* the unified `Run/RunStep` tree, both written on every send —
  `RequestExecutionService.cs:79,112-135`) but the Runner shows only the flat list; replay re-runs
  without profile auth.
- **Recommendation:** Consolidate on the unified run tree; add save-as-endpoint/duplicate.

## S-12 · Create Environment / Profile / Variable ✅
- **Observed:** Full CRUD (`ProfilesPanelViewModel`). **Secrets are strongly encrypted:**
  **AES-256-GCM** (authenticated, tamper-detecting) with the master key protected by **DPAPI**
  (`AesSecretProtector.cs`, `DpapiKeyStore.cs`) — better than the "AES/DPAPI" doc shorthand implies.
  Ciphertext is never surfaced back; all six `VariableScope` values are selectable.
- **Pain points (S3):** Editor offers **Workflow / Local / WorkflowOutput** scopes, but the seeder
  only materializes Global/Workspace/Environment — a variable in the other three **silently never
  loads**, with no feedback. Environments capture only Name + Kind (no base URL/metadata — thin labels).
- **Recommendation:** Hide/guard engine-internal scopes in the editor; enrich environments.

## S-13 · Switch Environment 🟡
- **Observed:** Toolbar switcher persists active env + broadcasts `EnvironmentsChangedMessage`
  (`EnvironmentSwitcherViewModel.cs:93-104`). **Re-resolution happens only for the ad-hoc Runner**
  (`RequestExecutionService` → `VariableScopeSeeder`). **Switching environment has no effect on
  workflow execution** (see S-16). So the switcher is honored by single requests, silently ignored by
  workflows. (S2 disconnect.)

## S-14 · Switch Profile ("Run As") 🔴 — **S1**
- **Goal:** Execute a request/workflow as a chosen role identity.
- **Observed:** Engine plumbing is real and correct — `RequestNodeHandler.cs:54,81-90` applies
  `IAuthApplicator` for `config.ProfileId`; `AuthApplicator` maps Bearer/Basic/ApiKey and decrypts at
  call time. **But there is no way for a user to set a profile:** the Api-node inspector exposes only
  Method/Url/Body (`NodePropertiesViewModel.cs:52-59`), `AddApiNodeFromEndpointAsync` never sets
  `ProfileId`, and **there is no active-profile switcher anywhere** (only the environment switcher).
  Net: `config.ProfileId` is always null → the entire "Run As" feature is **unreachable dead code**.
- **Impact:** Role/permission testing — a headline differentiator — is not usable at all.
- **Recommendation (S1):** Add a Profile picker on requests + Api nodes, and/or a toolbar Profile ▾.

## S-15 · Create Workflow ✅
- **Observed:** New/Rename/Delete + open via `OpenWorkflowMessage`. **Palette drag-drop works**;
  **endpoint drag from Explorer → auto-configured Api node works** (`WorkflowEditorViewModel.cs:172-205`);
  connections validated by `IConnectorValidator`; **undo/redo on every edit**; per-kind inspector.
- **Pain points (S3):** **No `F5`** (Run button only); new Assertion node gets a **null Config** so the
  inspector shows nothing until first edit (`NodeViewModelFactory:52-60`); **no dirty indicator**;
  manual save.

## S-16 · Execute Workflow 🟡 — **S1 (silent variable failure)**
- **Observed:** Run resets nodes to Pending, runs the engine with live `IProgress<NodeRunResult>`
  per-node status, persists a run tree; **failure policy + timeout + cancellation exist in the
  engine.** **But:**
  - 🔴 **Empty variable context.** The designer runs `RunAsync(...)` **with no context**
    (`WorkflowEditorViewModel.cs:351-353`); engine falls back to `new WorkflowContext()`. `VariableScopeSeeder`
    is **never invoked on any workflow path** (also true for `TestSuiteExecutor`, `StressOrchestrator`,
    `RunReplayService`). Unresolved `{{tokens}}` become **empty strings silently** (`VariableResolver.cs:20-25`).
    → Workflows can't use environments/variables at all, with no warning. **This is the flagship broken journey.**
  - 🟡 **No failure-policy / timeout UI** (always `WorkflowRunOptions.Default`); engine supports more.
  - 🟡 **No "Open in Timeline"** after a run — only a status-bar "Run {status}"; the documented drill
    handoff isn't wired from the editor.
- **Recommendation (S1):** Seed workflow context via `IVariableScopeSeeder`; warn on unresolved tokens;
  wire Open-in-Timeline; expose failure policy.

## S-17 · Stress Test 🟡
- **Observed:** `StressRunnerViewModel` drives `IStressOrchestrator` with Sequential/Loop/Concurrent,
  Run/Stop **is** cancellable (`IncludeCancelCommand`), live TPS/RPS + P50/P95/P99 + error rate stream
  in; **results persist and appear in Timeline/Dashboard** (`StressOrchestrator.cs:89-90`).
- **Pain points (S2):** **No charts** — numeric tiles only, despite the "visual stress testing"
  differentiator and the Sprint-13 LiveCharts2 expectation (`LiveMetricsViewModel.cs:8` "charts land in
  Sprint 13"). **No context-menu launch** ("Stress this endpoint" absent); target is a self-populated
  combo. Not replayable (by design).
- **Recommendation:** Add P95/P99 + failure-rate charts; add context-menu launch with prefilled target.

## S-18 · Dashboard 🟡
- **Observed:** Five live-refreshing widgets (KPI, success/fail donut, latency line, slowest &
  most-called). **But:** **no time-window filter and no environment filter exist in the query model**
  (`DashboardModels.cs:49-59` has only Source/MaxRuns/TopN); the VM hardcodes `new DashboardQuery()`
  (`DashboardViewModel.cs:74`) so even the `Source` filter isn't surfaced; and **clicking a chart does
  nothing** — no navigation command → **no drill-through to Timeline.**
- **Pain points (S2):** The Grafana-style "filter → read → drill" loop is broken at both filter and drill.
- **Recommendation:** Add time-window + environment filters; wire chart click → Timeline.

## S-19 · Timeline (drill + replay) ✅
- **Observed:** Runs newest-first from `IRunStore`; Loop/Parallel step tree rebuilt for drill-down;
  live refresh; reachable via View → Execution Timeline. **Replay works for request/workflow, correctly
  unsupported for stress.**
- **Pain points (S3):** `CanReplay()` is only `SelectedRun is not null` (`TimelineViewModel.cs:115`), so
  Replay stays **enabled on stress runs** and clicking it drops a `StressReplayUnsupported` error into
  the status bar — a dead action that should be disabled.

## S-20 · Logs ✅
- **Observed:** `LogViewerViewModel` reads persisted Serilog events; filter by min-level/source/text;
  auto-refresh. Errors from failed requests/workflows/stress surface here (DB sink is source-agnostic).
  No functional break found.

## S-21 · Settings ⬜ — **S2**
- **Observed:** **No general Settings/Preferences screen.** The only surface is the Backup dialog
  (auto-backup toggle, retention, restore list). Theme lives only in the View menu; no place for HTTP
  timeouts, proxy, plugin config, keymap, or default behaviors.
- **Recommendation:** Dedicated searchable Settings screen (VSCode-style) — already a documented `(gap)`.

## S-22 · Export Workspace ✅
- **Observed:** File → Export Package → `IWorkspacePackageService.ExportAsync` (Export.ApiStudio
  `WorkspacePackager`); suggests `<name>.apistudio`; reports size or a failure dialog; gated on an open
  workspace.
- **Pain points (S3):** Thin feedback — status line + final size, no progress bar for large workspaces.

## S-23 · Reopen Exported Workspace ✅
- **Observed:** File → Import Package → prompts source + new location → `ImportAsync` → shell refresh +
  round-trip report. **Cross-machine secret re-prompt flag honored** (`SecretsNeedReprompt`, `MissingPlugins`
  reported in a dialog — `ShellViewModel.cs:355-375`; same on backup Restore).
- **Pain points (S3):** Re-prompt is a **notice only** — there is no guided re-entry flow for the
  affected secrets afterward; the user must hunt them down manually.

## S-24 · Create & Run Test Case 🟡
- **Observed:** Assertion types are **plugin-driven** (json/regex/schema); run single case + suite →
  hierarchical pass/fail tree via `ShowTestResultsMessage`. **But:** target is **not prefilled and not
  reachable from any context menu** (chosen from a flat combo of *all* endpoints+workflows); the **Test
  Cases panel has no menu entry** (dead-end, same as S-26); **no results export** (JUnit/HTML) — report
  is in-memory counts only (`ITestReportBuilder.cs:6-33`).
- **Recommendation (S2):** "Add Test Case" from endpoint/workflow context menu with target prefilled;
  fix the panel dead-end; add JUnit/HTML export.

## S-25 · Attachments ⬜ — **S3**
- **Observed:** Packaging plumbing exists (`Export.ApiStudio/AttachmentStore.cs`) but **grep for
  `Attachment` across the entire UI project returns zero matches.** There is no way to add, view, or
  open an attachment. Confirmed as documented.

## S-26 · Navigate Between Areas 🟡 — **S2**
- **Observed:** The **only** navigation surface is the **View menu** + panel-emitted messages. No
  Activity Rail / Command Palette / Quick-Open / global search. Only `Ctrl+N/O/S` shortcuts exist.
  **Test Cases is absent from the menu → unreachable once closed.** Dashboard/Timeline/Stress commands
  lack `CanExecute = IsWorkspaceOpen` guards, so they can open with no workspace.
- **Recommendation (S2):** Activity Rail + shortcuts; add Test Cases to the menu; guard document commands.

## S-27 · Workflow Node Data-Mapping ⬜ — **S2**
- **Observed:** The `Mapping` field persists on `ConnectionViewModel`/`WorkflowEdge` and round-trips in
  `GraphMapper`, **but** `Connect` always sets `mapping = null`, there is **no authoring UI**, and the
  **engine never reads `edge.Mapping`** (`WorkflowEngine.SelectNext:203-225`). So even a hand-persisted
  mapping is inert. Data does not flow between nodes visually — the core "workflow-first" promise is
  only half-delivered. Also confirmed missing: **Switch node** and **Variable node** (enum values exist
  in `DomainEnums.cs:115-116`, but no handler/DI/palette entry → unusable).
- **Recommendation (S2):** Implement drag output→input mapping end-to-end (UI + engine); build or hide
  Switch/Variable nodes.
