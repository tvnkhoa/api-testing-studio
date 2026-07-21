# UI_BENCHMARK.md — UX Foundation & Design System

> The **why** behind the interface. This document is the official UI/UX guideline for
> **API Testing Studio**, derived from mature developer tools. It sits above the implementation
> docs: read it first, then `UI_GUIDELINES.md` (WPF/MVVM/XAML rules) and the module specs in
> `UI/*` (`Explorer.md`, `DockLayout.md`, `Runner.md`, `WorkflowDesigner.md`, `Dashboard.md`,
> `ProfilesAndEnvironments.md`, `Themes.md`) for the **how**. It never contradicts them —
> where it recommends something not yet built, that item is labelled **(gap)**.

## Purpose & how to use this doc

We are building a **professional developer tool**, not a consumer app. The goal is not novelty:
it is *familiarity*. A backend developer or QA engineer who already lives in Rider, VSCode,
Postman, and Grafana should feel at home on day one. Therefore this document's central rule is:

> **Reuse proven patterns from the benchmark tools. Invent a new interaction only when no mature
> tool solves the problem — and justify it in writing.**

Benchmark set: **IDEs** — JetBrains Rider, Visual Studio, VSCode. **API testing** — Postman,
Bruno, Insomnia. **Workflow** — n8n, Node-RED. **Monitoring** — Grafana. **Logging** — Seq.
**Developer experience** — GitHub Desktop, Docker Desktop.

Audience: everyone who touches the UI — product, design, and engineering. Every UI PR should be
checkable against the **UX Rules**, **Interaction Rules**, and **Desktop UX Guidelines** here.

---

## Product Philosophy

### What makes these applications successful

They all treat the user as a **professional operating a workbench**, not a visitor being guided.
Concretely, the winners share these traits:

- **A single durable workspace/project as the root of everything.** Rider's solution, VSCode's
  folder, Postman's workspace, Grafana's org. State is scoped, persistent, and portable. — Our
  root is the **Workspace** (`Domain/Entities/Workspace.cs`, one `.atsdb` SQLite file).
- **Keyboard-first, mouse-optional.** Every frequent action has a shortcut and a discoverable
  command surface. Rider/VSCode's command palette is the archetype.
- **Dockable, persistent, resettable layout.** The user arranges panels once; the tool remembers.
  Rider, VS, VSCode, Grafana all do this. — We already persist an AvalonDock layout
  (`UI/DockLayout.md`, `DockManagerService`).
- **Progressive disclosure.** Simple by default, powerful on demand. Postman hides scripting,
  auth, and settings behind tabs; n8n hides node config behind a side panel. Nothing dumps its
  full complexity on first sight.
- **Dense but calm information design.** Seq and Grafana pack enormous data density yet stay
  scannable through strict alignment, restrained color, and semantic-only highlights.
- **Non-blocking, cancellable work.** Long operations never freeze the shell; progress and
  cancel are always visible. — Enforced by `UI_GUIDELINES.md` ("UI never blocks").
- **Dark-first, theme-consistent.** Developer tools default to dark and treat theming as a
  first-class system, not a skin. — Material Design light/dark via `ThemeManager`.
- **Honest, actionable feedback.** Errors say what failed and what to do next (Docker Desktop,
  GitHub Desktop excel here). Success is quiet; failure is specific.

### UX principles they share (our non-negotiables)

1. **Familiarity over invention.** Match the mental model developers already hold.
2. **One primary action per surface.** The obvious next step is always visually dominant.
3. **Everything reachable three ways:** menu, shortcut, and command palette / context menu.
4. **State is never conveyed by color alone.** Pair color with an icon and a label
   (`UI_GUIDELINES.md` accessibility; `RunStatusToBrushConverter` already labels status).
5. **The layout is the user's, not ours.** We ship a good default and a Reset; we never fight it.
6. **Consistency is a feature.** The same verb behaves identically everywhere (double-click,
   right-click, rename, delete).

### Interaction patterns that appear repeatedly

| Pattern | Seen in | Our surface |
|---|---|---|
| Command palette (`Ctrl+Shift+P`) | VSCode, Rider, VS | **(gap)** — recommend adding |
| Quick-open / go-to-anything (`Ctrl+P`) | VSCode, Rider | **(gap)** — recommend adding |
| Left tree explorer + tabbed center | Rider, VS, VSCode | `ServiceExplorerView` + AvalonDock documents |
| Activity rail (icon strip switching left panel) | VSCode, Postman | **(gap)** — recommend adding |
| Bottom problem/log dock | VSCode, Rider, Seq | `LogViewerView`, `TimelineView` |
| Node canvas + side property inspector | n8n, Node-RED | `WorkflowEditorView` + `NodePropertiesViewModel` |
| Time-window + filter chips over charts | Grafana | `DashboardView` filters |
| Structured log stream with filter DSL | Seq | `LogViewerView` (level/source/text filter) |
| Split request/response with sub-tabs | Postman, Insomnia | `ApiRunnerView` |
| Empty state with a primary CTA | Bruno, Docker Desktop, GitHub Desktop | Welcome/`WelcomeDocumentView` |

---

## Screen Map

The complete hierarchy. Legend: **[✓]** implemented · **[gap]** recommended, not yet built.

```
Workspace  (.atsdb — open/new/close; one at a time)                              [✓]
│
├── Service Explorer  (tool pane, left)                                          [✓]
│   └── Service → Endpoint Folder → Endpoint                                     [✓]
│
├── API Runner  (document pane, reused)                                          [✓]
│   ├── Request Builder (method · URL · query · headers · body · auth)           [✓]
│   ├── Response Viewer (status · timing · body via Monaco)                      [✓]
│   └── History (per-endpoint; replay · duplicate · save as endpoint)            [✓]
│
├── Workflows                                                                    [✓]
│   ├── Workflows panel (tool pane — list/open/create)                           [✓]
│   └── Workflow Designer (document pane, one per workflow — Nodify canvas)      [✓]
│       ├── Node palette                                                         [✓]
│       └── Node Properties inspector                                            [✓]
│
├── Testing                                                                      [✓]
│   ├── Test Cases panel (tool pane)                                             [✓]
│   └── Test Results (document pane — pass/fail tree)                            [✓]
│
├── Stress Runner  (document pane — config + live metrics)                       [✓]
│
├── Analysis                                                                     [✓]
│   ├── Dashboard (document pane — KPIs, charts, rankings)                       [✓]
│   └── Timeline (document pane — execution history, drill-down, replay)         [✓]
│
├── Logs & History                                                              [✓]
│   ├── Log Viewer (tool pane — Serilog, level/source/text filter)              [✓]
│   └── Run History (inline in Runner; unified run tree Run→RunStep)            [✓]
│
├── Identity                                                                     [✓]
│   ├── Profiles (roles + encrypted credentials, "Run As")                       [✓]
│   ├── Environments (active set switcher in toolbar)                            [✓]
│   └── Variables (six scopes, {{...}} substitution)                             [✓]
│
├── Import / Export                                                              [✓]
│   ├── Import Wizard (cURL · OpenAPI · Swagger/URL · Scalar · Postman)          [✓]
│   ├── Export / Package (.apistudio ZIP)                                        [✓]
│   └── Backup & Restore                                                         [✓]
│
├── Attachments                                                                  [gap]  (packaging plumbing exists; no UI)
│
├── Settings  (dedicated screen)                                                 [gap]  (only BackupSettingsDialog today)
│
├── Command Palette                                                              [gap]
└── Global Search / Quick-Open                                                   [gap]
```

Reconciliation note: the **Settings** node is a first-class screen in this map but today
configuration only surfaces through `BackupSettingsDialog` + `IAppSettingsService`. Attachments,
Command Palette, and Global Search are likewise map-level intent, tracked in **Recommendations**.

---

## Navigation Flow

### How users move through the application

Navigation is **document-oriented and message-driven** — the correct model, already implemented.
Selecting an endpoint publishes `EndpointSelectedMessage`; `ShellViewModel` opens-or-focuses the
single shared `document.runner` pane. Opening a workflow publishes `OpenWorkflowMessage` → one
`document.workflow.{id}` pane. This mirrors how Rider/VSCode open editors from the tree.

Three ways to reach any surface (the target state):

1. **Tree / list** — Explorer, Workflows panel, Test Cases panel (mouse-driven browsing).
2. **Menu / toolbar / View menu** — discoverable, labelled.
3. **Command palette + quick-open** — keyboard-driven, fastest **(gap)**.

### How many clicks (target budgets)

| Task | Target | Notes |
|---|---|---|
| Open an endpoint into the Runner | 1 (double-click) or 2 (single-select + Enter) | as today |
| Send the current request | 0 clicks (`Ctrl+Enter`) | keymap gap |
| Jump to any endpoint by name | 2 keys (`Ctrl+P`, type) | quick-open gap |
| Run any command | 2 keys (`Ctrl+Shift+P`, type) | palette gap |
| Switch environment | 1 (toolbar `ComboBox`) | as today |
| Open Dashboard | 1 (View menu / rail) | as today |

Rule: **the most frequent action on any surface costs zero or one input.** Send, Run Workflow,
and Add Assertion are the hot paths — each gets a shortcut and a dominant button.

### What stays docked vs opens in tabs

Follow the Rider/VSCode split exactly:

- **Docked tool panes (persistent context):** Service Explorer (left), Workflows (left),
  Profiles/Environments/Variables (left or right), Test Cases (left), Log Viewer (bottom).
  These are *navigational and supporting* — always available, never the focus of work.
- **Tabbed document panes (the work itself):** API Runner (one reused pane), Workflow Designer
  (one per workflow), Dashboard, Timeline, Stress Runner, Test Results. These are *what the user
  is doing right now* and belong in the center where tabs, split, and history live.

This matches `UI/DockLayout.md` and needs no change — only the additions below.

---

## Dock Layout

Recommended optimal layout, grounded in Rider + VSCode + VS and reconciled with
`UI/DockLayout.md` (which already establishes the AvalonDock shell, document vs tool panes, and
persistence). The only structural addition is the **Activity Rail (gap)**.

```
┌───────────────────────────────────────────────────────────────────────────────┐
│  Toolbar:  New · Open · Save │ Env ▾ │ Profile ▾ │ Run ▸ │ Theme │ (search box) │
├──┬────────────────────────┬───────────────────────────────┬───────────────────┤
│A │  LEFT PANEL (tool)     │  CENTER (document tabs)        │  RIGHT PANEL (tool)│
│C │  Explorer / Workflows /│  [Runner] [Workflow] [Dash]…   │  Properties /      │
│T │  Test Cases / Profiles │                                │  Node Inspector /  │
│I │                        │  the active work surface       │  Request Details   │
│V │  (activity-rail        │                                │                    │
│I │   switches which       │                                │  (context-sensitive│
│T │   tool shows here)     │                                │   — hidden when     │
│Y │                        │                                │   empty)            │
├──┴────────────────────────┴───────────────────────────────┴───────────────────┤
│  BOTTOM PANEL (tool):  Logs │ Timeline │ Test Results │ Problems                │
├─────────────────────────────────────────────────────────────────────────────┬─┤
│  Status Bar:  workspace · env · connection · background task · run summary    │▲│
└───────────────────────────────────────────────────────────────────────────────┘
```

- **Activity Rail (A, far left) — (gap).** A thin icon strip (VSCode/Postman) that switches the
  LEFT panel between Explorer, Workflows, Test Cases, Profiles. Replaces hunting in the View menu
  and gives each major area a memorable home + shortcut (`Ctrl+1..5`).
- **Left panel — tool.** Hosts one navigational tree/list at a time. Default: Explorer.
  Rationale: developers scan structure on the left (every IDE); keeping one panel visible
  preserves center width for work.
- **Center — documents.** Tabbed, reorderable, splittable. The Runner, each Workflow Designer,
  Dashboard, Timeline, Stress, and Test Results live here. This is where focus and tab history
  belong (Rider/VSCode).
- **Right panel — tool, context-sensitive.** The **Properties / Inspector** surface: node config
  in the Workflow Designer (already `NodePropertiesViewModel`), request/response metadata in the
  Runner, assertion detail in Testing. Grafana/n8n/VS all put the inspector on the right. Hidden
  when nothing is selected — never an empty gray slab.
- **Bottom panel — tool.** Logs (Seq-style stream), Timeline, Test Results, future Problems.
  Collapsible; the VSCode bottom-dock model. Errors surface here, not in modal popups.
- **Status Bar.** Persistent, quiet, information-dense (VSCode/Rider): workspace name,
  active environment, connection state, background-task spinner, last-run summary. Already
  `StatusBarViewModel` — extend, don't replace.
- **Toolbar.** Global, low-churn actions only: workspace ops, environment + profile switchers,
  a primary Run control, theme, and a **search box (gap)**. Avoid a crowded ribbon.

**Why this layout is optimal:** it is the exact spatial contract of every IDE the audience
already uses — structure left, work center, detail right, diagnostics bottom, state in the status
bar. Zero learning cost, maximum center real estate, and it maps 1:1 onto the AvalonDock
document/tool split we already ship.

---

## Feature Mapping

For every module, the best-in-class exemplar and why we follow it.

| Module | Best UX | Why we follow it |
|---|---|---|
| **Service Explorer** | **Rider** (Solution Explorer) | Nested tree, type-badged nodes, inline rename, rich context menu, incremental filter. Verb badges are our analog of Rider's file-type icons. → `ServiceExplorerViewModel`, `UI/Explorer.md` |
| **Request Builder** | **Postman** (+ Insomnia) | The definitive request UI: method dropdown + URL bar + tabbed Params/Headers/Body/Auth, key-value rows with enable toggles. Insomnia contributes calm density. → `RequestBuilderViewModel` |
| **Body / Response editor** | **VSCode** (Monaco) | We literally embed Monaco. Syntax highlight, fold, format, find — the editor developers trust. → `MonacoEditorViewModel`, `UI/Runner.md`, `ADR-0009` |
| **Workflow Designer** | **n8n** (+ Node-RED) | Node canvas, drag-from-palette, click-node → right-panel config, live per-node run status. n8n's execution-status-on-canvas is our exact model. → `WorkflowEditorViewModel`, `UI/WorkflowDesigner.md` |
| **Dashboard** | **Grafana** | Time-window + filter over a widget grid, semantic-colored panels, live refresh. → `DashboardViewModel`, `UI/Dashboard.md` |
| **Timeline / run history** | **Grafana + GitHub Desktop** | Chronological runs with drill-down and replay; GitHub Desktop's history-detail split informs the drill-down. → `TimelineViewModel` |
| **Logs** | **Seq** | Structured event stream, level/source/text filtering, expandable rows, no secrets. → `LogViewerViewModel`, `UI/Logging` (`FEATURES/Logging.md`) |
| **Stress Runner** | **Grafana + k6-style dashboards** | Config-then-live-metrics: latency percentiles (P95/P99), TPS/RPS, failure rate as running charts. → `StressRunnerViewModel` + `LiveMetricsViewModel` |
| **Profiles / Environments / Variables** | **Postman** (environments) + **Insomnia** | Named environment switcher in the toolbar, masked secrets, scoped variable precedence. → `ProfilesPanelViewModel`, `UI/ProfilesAndEnvironments.md` |
| **Import** | **Insomnia / Postman** import wizards | Auto-detect source format, preview, then merge. → `ImportWizardDialog`, `FEATURES/Import.md` |
| **Settings** | **VSCode** (settings UI) | Searchable, categorized, two-pane settings with live descriptions — the model for our **(gap)** Settings screen. |
| **Command palette / quick-open** | **VSCode / Rider** | Fuzzy command + go-to-anything. The single highest-leverage **(gap)**. |
| **Empty states / onboarding** | **Bruno + Docker Desktop + GitHub Desktop** | Friendly first-run with one primary CTA and a short "what next." → Welcome document |
| **Diff / change review** | **GitHub Desktop** | Model for future request/run diffing — clear two-pane compare. |

---

## UX Rules (design system)

The token system already exists in `src/ApiTestingStudio.UI/Themes/Tokens.xaml` and
`UI/Themes.md`. **These rules make it the single source of truth and forbid the hardcoding that
exploration found in views.**

### Spacing, margins, padding

- **Use only the 4-based scale** (`Spacing.Xs`=4, `Sm`=8, `Md`=12, `Lg`=16, `Xl`=24, `Xxl`=32).
  Never a literal margin/padding in a view. This is the single most-violated rule today.
- Control internal padding: `Sm`–`Md`. Group gaps: `Md`–`Lg`. Panel/dialog padding: `Lg`.
  Section separation: `Xl`.
- One alignment grid per surface. Left edges line up; labels and fields share a column.

### Typography

- **Use only the ramp** (`Typography.Headline`=28, `Title`=20, `Body`=14, `Caption`=12).
- Headline: screen/empty-state titles only. Title: panel/section headers. Body: default. Caption:
  metadata, timestamps, helper text.
- Monospace (the Monaco/editor font) for URLs, headers, JSON, log payloads, and any literal value.
- Never encode meaning in bold weight alone — pair with hierarchy or an icon.

### Icons

- **Material Design icon set only** (`UI_GUIDELINES.md`). One concept = one icon, everywhere.
- Every icon-only button has `AutomationProperties.Name` + a tooltip (accessibility rule).
- HTTP verbs use the shared `HttpVerbToBrushConverter` badge — consistent color across Explorer,
  Runner tabs, and workflow Api nodes.

### Color & semantic tokens

- **Use semantic keys, never raw hex at the call site** (`Semantic.Success/Error/Warning/Info`).
  Exploration found `#22000000` / `#33888888` literals in views — replace with tokens.
- **State is never color alone.** Success/Error/Warning always carry an icon + text label
  (`RunStatusToBrushConverter` already returns a labelled status).
- Accent = Material primary; keep the palette restrained so semantic colors stand out.

### Per-surface rules

- **Dialogs** — modal via `IDialogService`; one clear title; primary action bottom-right,
  Cancel to its left; `Esc` cancels, `Enter` confirms; never nest modals. Padding `Lg`.
- **Buttons** — exactly one primary (filled) action per surface; secondary = outlined; tertiary =
  text. Destructive = text/outlined in `Semantic.Error`, never the default focus.
- **Toolbar** — global low-churn actions only; icon + short label for primary, icon-only
  (tooltipped) for the rest; group with separators; no more than ~7 top-level items.
- **Forms** — single column, labels above fields; validation inline under the field, not in a
  popup; masked secrets via the existing `PasswordBoxBehavior`; disable the submit until valid.
- **Property panels** — right-docked, key/value grid, grouped by category, section headers in
  `Title`; edits apply immediately or via an explicit Apply — pick one and be consistent.
- **Lists** — hover affordance, single-click selects, double-click activates, right-click menu;
  virtualize long lists (Explorer already does).
- **Tables / DataGrid** — right-align numerics, monospace values, sortable headers, zebra optional
  but subtle; keep row height compact (Seq/Grafana density) but tap-friendly.
- **Context menus** — verbs only, ordered by frequency, destructive actions last and separated;
  every item mirrors a keyboard shortcut where one exists.
- **Tabs (documents)** — show a dirty indicator, closeable, reorderable, middle-click closes;
  never silently discard unsaved edits.

---

## Interaction Rules

Match VSCode/Rider conventions so muscle memory transfers.

| Interaction | Rule |
|---|---|
| **Double-click** | Activate/open the primary target (endpoint → Runner; workflow → Designer). Never destructive. |
| **Single-click** | Select only; updates the right-panel inspector, opens nothing. |
| **Right-click** | Context menu of verbs for the item under the cursor; selects it first if unselected. |
| **Drag & drop** | Reorder within a tree; drag an endpoint onto the workflow canvas to create a pre-configured `Api` node (already supported); drag tabs to reorder/split. Show a drop indicator; forbid invalid drops visibly. |
| **Selection** | Single-click; `Ctrl+click` toggles, `Shift+click` ranges. Selection is always visible. |
| **Multi-selection** | Enable batch delete/move/run where meaningful (e.g. multiple test cases). Actions apply to the whole selection or none. |
| **Undo / Redo** | `Ctrl+Z` / `Ctrl+Y`. Mandatory on the Workflow Designer (already `IUndoRedoService`); extend to structural edits elsewhere. Never undo a network send — that is history/replay, not undo. |
| **Copy / Paste** | `Ctrl+C` / `Ctrl+V`. Copy an endpoint/node as portable JSON; paste into a compatible target. Copy response body / log row as text. |
| **Delete** | `Del`. Always confirm irreversible deletes (see Confirmation Dialogs); soft where possible. |
| **Rename** | `F2` inline in trees/lists (Rider model), not a modal, when practical. |
| **Search** | `Ctrl+F` filters the focused list/tree/editor in place; `Ctrl+P` quick-open **(gap)**; `Ctrl+Shift+P` command palette **(gap)**; `Ctrl+Shift+F` future workspace-wide search. |

### Proposed keymap (gap — align to VSCode/Rider)

`Ctrl+N/O/S` workspace ops (exists) · `Ctrl+Enter` send request · `F5` run workflow/test ·
`Ctrl+P` quick-open · `Ctrl+Shift+P` command palette · `Ctrl+F` find-in-view ·
`Ctrl+1..5` activity-rail panels · `Ctrl+W` close tab · `Ctrl+Tab` next tab · `F2` rename ·
`Del` delete · `Esc` cancel dialog/close overlay.

---

## User Journeys

Each journey minimizes clicks and cognitive load and maps to real commands/messages.

1. **Create Workspace** — `Ctrl+N` → name + location → empty workspace opens with a Welcome
   document showing three CTAs (Import APIs · Add Service · Open sample). *Target: 2 inputs to a
   usable workspace.*
2. **Import APIs** — Toolbar Import → Import Wizard auto-detects (paste cURL / pick OpenAPI file /
   enter Swagger URL / Postman) → preview detected services & endpoints → Merge. Explorer
   populates. *No format picking required (`ISourceFormatDetector`).*
3. **Run Request** — Double-click an endpoint in Explorer → Runner opens/focuses with defaults
   prefilled → adjust body/headers → `Ctrl+Enter` → response + timing render; run is recorded to
   history. *1 double-click + 1 shortcut.*
4. **Create Workflow** — Workflows panel → New → empty Designer tab → drag endpoints from Explorer
   onto the canvas (auto-configured `Api` nodes) → connect ports → click a node to edit config in
   the right inspector. *Drag-driven, no dialogs.*
5. **Execute Workflow** — In the Designer, `F5` / Run → per-node status animates live
   (`IProgress<NodeRunResult>`), failures highlighted with icon+label → open the run in Timeline
   for drill-down. *1 shortcut to run, live feedback.*
6. **Create Test Case** — From an endpoint or workflow context menu → "Add Test Case" → target is
   prefilled → add assertions (type list from loaded `IAssertion` plugins) → Save. *Assertions are
   the only real input.*
7. **Run Stress Test** — From an endpoint/workflow → "Stress" → Stress Runner tab with mode
   (Sequential/Loop/Concurrent) + counts → Start → live P95/P99, TPS/RPS, failure-rate charts →
   results persisted. *Config is one form; results are live.*
8. **Analyze Dashboard** — Open Dashboard → set time window + environment filter → read KPIs,
   success/failure donut, slowest/most-called rankings; live-refresh as new runs complete → click a
   point to jump to Timeline. *Filter, read, drill.*
9. **Export Workspace** — File → Export → choose `.apistudio` location → package written
   (DB + attachments + manifest). Same-machine secrets round-trip; cross-machine flags
   `SecretsNeedReprompt`. *One dialog; honest about secrets.*

---

## Desktop UX Guidelines

### Do

- Ship a sensible default layout and a **Reset Layout** (exists); let users rearrange freely.
- Make the primary action on every surface visually dominant and keyboard-triggerable.
- Give every list/tree an empty state with a CTA and every long op a progress + cancel.
- Keep the shell responsive — all I/O async, marshal to UI thread only to update.
- Label state with icon + text + color together.

### Don't

- Don't block the UI thread; don't show a spinner with no cancel.
- Don't hardcode hex, margins, or font sizes in views — use tokens.
- Don't convey pass/fail by color alone.
- Don't stack modal dialogs or interrupt work with non-critical popups — use the status bar or
  bottom Logs panel.
- Don't invent a bespoke control where a standard/Material one exists.

### Best practices

- Confirm before irreversible actions; make everything else undoable.
- Preserve user context across sessions (open documents, last environment) — a stated future item.
- Prefer inline editing (rename, key-value rows) over modal round-trips.

### Common mistakes to avoid

- A crowded toolbar/ribbon (put rare actions in menus/palette).
- Right panel shown empty (hide it when nothing is selected).
- Error dialogs that state the failure but not the fix.
- Inconsistent verbs (double-click sometimes opens, sometimes renames).

### Accessibility (raise from bullets to a standard)

- **Contrast:** meet **WCAG AA** (4.5:1 text, 3:1 large text / UI glyphs) in **both** themes.
- **Keyboard:** every action reachable without a mouse; visible focus ring; logical tab order.
- **Screen reader:** `AutomationProperties.Name` on all icon-only controls; meaningful names on
  panes and charts.
- **Scaling:** respect system font scaling; relative sizing only (`UI_GUIDELINES.md`).
- **Motion:** keep canvas/chart animation subtle and respect reduced-motion where detectable.

### Visual hierarchy

Size (type ramp) → weight → color (semantic) → spacing. One dominant element per view; group
related controls with whitespace, not boxes, where possible.

### States (define all four for every data surface)

- **Empty** — friendly title + one-line explanation + primary CTA (Bruno/Docker/GitHub Desktop).
- **Loading** — inline skeleton or progress + cancel; never a frozen shell.
- **Error** — what failed, why, and the next action; recoverable in place; details to Logs.
- **Populated** — the normal dense, scannable view.

### Confirmation dialogs

Only for irreversible/destructive actions. State the consequence, name the target, default focus
on the safe choice, destructive button in `Semantic.Error`. `Esc` cancels.

### Notifications

Transient, non-blocking (status bar / toast), dismissible; errors persist in the Logs panel.
Never a modal for a background result.

### Progress indicators

Determinate where length is known (import, stress run, workflow); indeterminate otherwise; always
paired with a cancel; long runs stream partial results (workflow node status, stress metrics).

---

## UI Component Library

Where each component belongs, tied to actual views.

| Component | Use for | Where |
|---|---|---|
| **Buttons** | One primary per surface; secondary/tertiary as outlined/text | Toolbars, dialogs, forms |
| **Cards** | KPI tiles, dashboard widgets, welcome CTAs | `DashboardView`, `WelcomeDocumentView` |
| **Panels (tool)** | Persistent navigation/context | Explorer, Workflows, Profiles, Logs |
| **Dock windows (document)** | The active work surface, tabbed | Runner, Designer, Dashboard, Timeline, Stress |
| **Dialogs** | Focused create/edit + confirmations | `Views/Dialogs/*` via `IDialogService` |
| **Property grid / inspector** | Context-sensitive detail editing | Right panel; `NodePropertiesViewModel` |
| **JSON viewer / editor** | Request/response bodies, log payloads | Monaco via WebView2 (`MonacoEditorViewModel`) |
| **Splitters** | Resizable panel boundaries | AvalonDock manages these |
| **DataGrid** | Tabular results, metrics, assertions | Test Results, stress samples |
| **TreeView** | Hierarchical navigation | `ServiceExplorerView` (Service→Folder→Endpoint) |
| **Tabs** | Multiple open documents; sub-sections in Runner | AvalonDock documents; Runner Params/Headers/Body |
| **Charts** | KPIs, trends, distributions, live metrics | LiveCharts2 in Dashboard + Stress |
| **Timeline** | Chronological runs with drill-down | `TimelineView` |
| **Status bar** | Persistent ambient state | `StatusBarViewModel` |
| **Toolbar** | Global low-churn actions | Host `ToolbarViewModel` |
| **Activity rail (gap)** | Switch the left tool panel | New — VSCode/Postman model |
| **Command palette (gap)** | Fuzzy command execution | New — VSCode/Rider model |

---

## Recommendations — Top 20 UX improvements (ranked by impact)

Each: **problem → benchmark precedent → impact → rough effort.** Do these before adding net-new
features; they raise the whole product's professional feel.

1. **Command palette (`Ctrl+Shift+P`).** No unified command surface today. → VSCode/Rider. →
   Massive discoverability + speed gain across every module. → **M.**
2. **Global quick-open / go-to-anything (`Ctrl+P`).** No fast jump to endpoint/workflow. →
   VSCode/Rider. → Removes tree-hunting; scales with workspace size. → **M.**
3. **Adopt design tokens everywhere; ban hardcoded hex/margins.** Views hardcode `#22000000`,
   literal margins/sizes despite `Tokens.xaml`. → Every mature tool has one style system. →
   Instant visual consistency + trivial re-theming. → **M** (sweep views).
4. **Central control-style layer (Button/TextBox/Card/DataGrid).** No shared styles today. →
   VS/VSCode design systems. → Kills per-view drift. → **M.**
5. **Activity rail for the left panel (`Ctrl+1..5`).** Panels are buried in the View menu. →
   VSCode/Postman. → Memorable homes + one-key switching. → **M.**
6. **Dedicated Settings screen.** Only a backup dialog exists. → VSCode settings UI. →
   Central, searchable configuration; room to grow (theme, keymap, defaults). → **M.**
7. **Fuller, documented keymap.** Only `Ctrl+N/O/S` today. → Rider/VSCode. → Keyboard-first
   workflow; publish the map in-app. → **S.**
8. **Consistent empty / loading / error states across all panels.** → Bruno/Docker/GitHub Desktop.
   → Fewer dead-ends, clearer first run. → **M.**
9. **Right-panel Properties/Inspector convention.** Inspector exists only in the Designer. →
   n8n/Grafana/VS. → One predictable place for detail editing. → **M.**
10. **Status bar enrichment.** Add run summary, background-task progress, env/profile at a glance.
    → VSCode/Rider. → Ambient awareness without popups. → **S.**
11. **Inline rename (`F2`) in trees/lists.** → Rider. → Removes modal round-trips. → **S.**
12. **In-place find (`Ctrl+F`) on every list/tree/editor.** Explorer filters; generalize it. →
    VSCode/Seq. → Consistent search muscle memory. → **S.**
13. **Attachments UI.** Packaging plumbing exists; no UI. → — → Completes the workspace model. →
    **M.**
14. **Toast/notification center (non-blocking) + errors persisted to Logs.** → GitHub Desktop/
    Docker Desktop. → Feedback without interrupting work. → **M.**
15. **Restore open documents + last environment across sessions.** Stated future item. →
    Rider/VSCode. → Users resume instantly. → **M.**
16. **Named layout presets ("Design", "Run", "Analyse").** → Rider layouts / Grafana. →
    Fast context switching. → **S–M.**
17. **Accessibility pass to WCAG AA + keyboard audit.** Currently bullet-level. → Platform norm. →
    Broader usability + compliance. → **M.**
18. **Drag output→input data mapping in the Designer.** Noted post-14 gap. → n8n. → Unlocks real
    data flow between nodes. → **L.**
19. **Report/result export (JUnit/HTML) for test & stress runs.** Deferred. → CI tooling norms. →
    Makes results shareable/CI-friendly. → **M.**
20. **Run/request diffing.** Deferred. → GitHub Desktop diff. → Spot regressions between runs. →
    **L.**

Effort key: **S** ≈ days · **M** ≈ 1–2 weeks · **L** ≈ multi-sprint.

---

## See also

- `UI_INFORMATION_ARCHITECTURE.md` — the IA (content model, functional areas, sitemap, labeling)
  and the end-to-end user flows derived from this document.
- `UI_GUIDELINES.md` — WPF/MVVM/XAML rules, accessibility baseline, async rules.
- `UI/DockLayout.md` · `UI/Explorer.md` · `UI/Runner.md` · `UI/WorkflowDesigner.md` ·
  `UI/Dashboard.md` · `UI/ProfilesAndEnvironments.md` · `UI/Themes.md` — module implementation specs.
- `DECISIONS/ADR-0008-Shell-UI-Layout-Theme-Persistence.md` · `DECISIONS/ADR-0009-Api-Runner-Execution-And-Monaco.md`.
- `CLAUDE.md` — project constitution and technology stack.
