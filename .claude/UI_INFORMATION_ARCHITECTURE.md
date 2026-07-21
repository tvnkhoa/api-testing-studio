# UI_INFORMATION_ARCHITECTURE.md — Information Architecture & User Flows

> The **structure** and the **movement**. This document defines how content is organized (IA) and
> how users move through it (user flows) for **API Testing Studio**. It is derived from
> `UI_BENCHMARK.md` (the UX foundation) and grounded in the real domain model. Read
> `UI_BENCHMARK.md` first for the *why*; this doc is the *what goes where* and *what happens next*.
> Items not yet built are labelled **(gap)**, consistent with the benchmark.

---

## Part 1 — Information Architecture

### IA principles (from the benchmark)

1. **Workspace is the root of everything.** All content is scoped to one open Workspace; there is
   no global, cross-workspace state except app preferences and recent-workspace pointers.
2. **Organize by the user's mental model, not the code's.** Group screens into a small set of
   *functional areas* a developer already recognizes (Explore APIs → Automate → Verify → Load →
   Analyze → Diagnose), each with a memorable home.
3. **One primary object per area; supporting objects hang off it.** APIs area centers on the
   Endpoint; Automate on the Workflow; Verify on the Test Case; Analyze on the Run.
4. **Stable spatial home for every content type.** Navigation lives left, work lives center,
   detail lives right, diagnostics live bottom, ambient state lives in the status bar
   (see `UI_BENCHMARK.md` → Dock Layout).
5. **Three routes to any content:** browse (tree/list), command (menu/toolbar/palette), and
   direct (quick-open by name). Findability is a first-class IA concern, not an afterthought.
6. **Consistent, unambiguous labels.** One term per concept, everywhere (see Labeling System).

### Content model (entity taxonomy)

Everything below lives inside a single Workspace (`.atsdb`). This is the authoritative content
map the navigation is built on.

```mermaid
erDiagram
    WORKSPACE ||--o{ SERVICE : contains
    WORKSPACE ||--o{ PROFILE : contains
    WORKSPACE ||--o{ ENVIRONMENT : contains
    WORKSPACE ||--o{ VARIABLE : contains
    WORKSPACE ||--o{ WORKFLOW : contains
    WORKSPACE ||--o{ TEST_SUITE : contains
    WORKSPACE ||--o{ RUN : records
    WORKSPACE ||--o{ STRESS_RUN : records
    WORKSPACE ||--o{ LOG_EVENT : records
    WORKSPACE ||--o{ ATTACHMENT : holds

    SERVICE ||--o{ ENDPOINT_FOLDER : groups
    SERVICE ||--o{ ENDPOINT : groups
    ENDPOINT_FOLDER ||--o{ ENDPOINT_FOLDER : nests
    ENDPOINT_FOLDER ||--o{ ENDPOINT : holds

    WORKFLOW ||--o{ WORKFLOW_NODE : "graph of"
    WORKFLOW ||--o{ WORKFLOW_EDGE : connects
    WORKFLOW_NODE ||--o| ENDPOINT : "Api node references"

    TEST_SUITE ||--o{ TEST_CASE : groups
    TEST_CASE ||--o{ ASSERTION : verifies
    TEST_CASE }o--o| ENDPOINT : "targets (either)"
    TEST_CASE }o--o| WORKFLOW : "targets (or)"

    RUN ||--o{ RUN_STEP : "tree of"
    RUN_STEP ||--o{ RUN_STEP : nests
    RUN }o--o| ENDPOINT : "source (request)"
    RUN }o--o| WORKFLOW : "source (workflow)"

    ENVIRONMENT ||--o{ VARIABLE : "scopes"
    PROFILE ||--o{ ENDPOINT : "Run As applies to"
```

Key facts the IA must respect:
- **Endpoints** belong to a **Service** and optionally nest in self-nesting **Endpoint Folders**.
- **Variables** resolve across **six scopes** in precedence order: Global → Workspace →
  Environment → Workflow → Local → WorkflowOutput.
- **Exactly one Environment is active** per workspace; switching re-resolves env-scoped variables.
- **Profiles** are *roles*; "Run As" swaps auth per request/workflow/node.
- **Test Cases target either an Endpoint or a Workflow** — never both.
- **Runs are a unified tree** (`Run → RunStep`) shared by request, workflow, and stress sources;
  Dashboard, Timeline, and Replay all read it.

### Functional areas (the top-level IA)

The product collapses into **8 functional areas**. Each maps to an Activity-Rail entry
(**(gap)** — see `UI_BENCHMARK.md`), a default left/bottom home, and the documents it opens.

| # | Area | User intent | Primary object | Left/bottom home (tool) | Opens (document) |
|---|---|---|---|---|---|
| 1 | **APIs** | Explore & call endpoints | Endpoint | Service Explorer (left) | API Runner |
| 2 | **Workflows** | Automate multi-step calls | Workflow | Workflows panel (left) | Workflow Designer (1/workflow) |
| 3 | **Testing** | Verify behavior | Test Case | Test Cases panel (left) | Test Results |
| 4 | **Stress** | Load & measure | Stress plan | (launched from APIs/Workflows) | Stress Runner |
| 5 | **Analysis** | Understand activity | Run | (launched from menu/rail) | Dashboard · Timeline |
| 6 | **Logs** | Diagnose | Log event / Run | Log Viewer (bottom) | — (History inline in Runner) |
| 7 | **Identity** | Model who/where/what | Profile · Environment · Variable | Profiles panel (left) + toolbar switchers | editor dialogs |
| 8 | **Settings** | Configure the tool | Setting | — | Settings screen **(gap)**; Backup dialog today |

### Navigation model

Three tiers, following the benchmark's Dock Layout.

- **Primary navigation — Activity Rail (gap).** A thin icon strip switching the left panel between
  areas 1–3 and 7 (`Ctrl+1..5`). Gives each area a fixed home and one-key access.
- **Secondary navigation — trees & lists** inside the left tool panel (Service tree, Workflows
  list, Test Cases list, Profiles/Environments/Variables tabs) and **document tabs** across the
  center for open work surfaces.
- **Utility navigation — global & ambient:** Toolbar (workspace ops, Environment ▾, Profile ▾,
  Run, theme, search box), Menu, Status Bar (workspace · env · connection · task · last run),
  and the **Command Palette + Quick-Open (gap)** for keyboard-driven jumps.

```mermaid
flowchart LR
    subgraph Utility[Utility navigation]
        TB[Toolbar]:::u
        MENU[Menu]:::u
        SB[Status Bar]:::u
        CP["Command Palette / Quick-Open (gap)"]:::gap
    end
    subgraph Primary["Primary nav — Activity Rail (gap)"]
        A1[APIs]:::gap
        A2[Workflows]:::gap
        A3[Testing]:::gap
        A7[Identity]:::gap
    end
    subgraph Secondary[Secondary navigation]
        TREE[Trees & lists<br/>left tool panel]:::s
        TABS[Document tabs<br/>center]:::s
        INSP[Inspector<br/>right tool panel]:::s
        DIAG[Logs / Timeline<br/>bottom panel]:::s
    end
    Primary --> TREE --> TABS --> INSP
    TABS --> DIAG
    Utility --> Primary
    classDef u fill:#1565C0,color:#fff;
    classDef s fill:#2E7D32,color:#fff;
    classDef gap fill:#EF6C00,color:#fff,stroke-dasharray:4 3;
```

### Sitemap / screen inventory

Legend: **[✓]** implemented · **[gap]** recommended. Type: **Doc** = document pane ·
**Tool** = dockable tool pane · **Dialog** = modal · **Shell** = frame chrome.

```
Workspace  (.atsdb)                                                       Shell   [✓]
├─ Shell chrome: Menu · Toolbar · Status Bar                              Shell   [✓]
├─ Activity Rail                                                          Shell   [gap]
├─ Command Palette · Quick-Open · Global Search                           Shell   [gap]
│
├─ APIs
│  ├─ Service Explorer (Service→Folder→Endpoint, filter, context menu)    Tool    [✓]
│  └─ API Runner (Request Builder · Response Viewer · History)            Doc     [✓]
│
├─ Workflows
│  ├─ Workflows panel (list · new · open)                                 Tool    [✓]
│  └─ Workflow Designer (canvas · palette · node inspector)               Doc     [✓]
│
├─ Testing
│  ├─ Test Cases panel                                                    Tool    [✓]
│  └─ Test Results (pass/fail tree)                                       Doc     [✓]
│
├─ Stress
│  └─ Stress Runner (config · live metrics)                               Doc     [✓]
│
├─ Analysis
│  ├─ Dashboard (KPIs · charts · rankings · filters)                      Doc     [✓]
│  └─ Timeline (chronological runs · drill-down · replay)                 Doc     [✓]
│
├─ Logs
│  └─ Log Viewer (level/source/text filter)                              Tool    [✓]
│
├─ Identity
│  ├─ Profiles panel (Profiles · Environments · Variables tabs)          Tool    [✓]
│  ├─ Environment switcher (toolbar)                                      Shell   [✓]
│  └─ Profile / Environment / Variable editors                           Dialog  [✓]
│
├─ Import / Export
│  ├─ Import Wizard (cURL · OpenAPI · Swagger/URL · Scalar · Postman)     Dialog  [✓]
│  ├─ Export / Package (.apistudio)                                       Dialog  [✓]
│  └─ Backup & Restore                                                    Dialog  [✓]
│
├─ Attachments                                                            —       [gap]
└─ Settings (dedicated, searchable)                                       Doc     [gap]
```

### Labeling system & taxonomy

Consistency prevents the "same concept, two names" trap the benchmark warns against.

| Canonical term | Means | Do **not** call it |
|---|---|---|
| **Workspace** | The `.atsdb` root container | Project, Collection |
| **Service** | Base-URL grouping of endpoints | Collection, Folder |
| **Endpoint Folder** | Optional nesting under a Service | Group, Directory |
| **Endpoint** | One callable operation (verb + path) | Request, Route (in UI) |
| **Request** | A single *execution* of an endpoint in the Runner | Call, Hit |
| **Profile** | A role identity + encrypted credentials ("Run As") | User, Account, Auth |
| **Environment** | A named variable set; one active at a time | Stage, Config |
| **Variable** | `{{...}}` substitution value, scoped | Param, Setting |
| **Workflow** | A node-graph automation | Pipeline, Flow (in UI), Scenario |
| **Node** | One step in a workflow | Block, Action, Task |
| **Test Case** | A verifiable check on an endpoint or workflow | Test, Spec |
| **Assertion** | One expected condition on a response | Check, Rule |
| **Run** | A recorded execution (request/workflow/stress) | Result, Job, Session |
| **Stress Run** | A load execution + metrics | Benchmark, Perf test |

Iconography follows the Material set with a fixed concept↔icon mapping; HTTP verbs always use the
shared `HttpVerbToBrushConverter` badge across Explorer, Runner tabs, and workflow Api nodes.

### State & persistence model (what the IA remembers)

| State | Scope | Persisted where |
|---|---|---|
| Content (services, endpoints, workflows, tests, runs…) | Workspace | `.atsdb` SQLite |
| Active Environment | Workspace | Settings row `active-environment-id` |
| Dock layout | Per user (global) | `dock-layout.xml` |
| Theme (light/dark) | Per user | `AppSettings.ThemeMode` |
| Recent workspaces | Per user | recent-workspaces store |
| Open documents & last environment across sessions | Per workspace | **(gap)** — recommended |
| Named layout presets ("Design/Run/Analyse") | Per user | **(gap)** — recommended |

---

## Part 2 — User Flows

### Master flow — how the areas interconnect

```mermaid
flowchart TD
    START([Launch app]) --> WS{Workspace open?}
    WS -- "No / first run" --> PICK[Open or Create Workspace]
    WS -- "Yes (recent)" --> HOME
    PICK --> HOME[Workspace open · Welcome document]

    HOME --> IMPORT[Import APIs]
    HOME --> EXPLORE[APIs · Service Explorer]
    IMPORT --> EXPLORE

    EXPLORE --> RUN[Run a Request]
    EXPLORE -.->|drag endpoints| WF[Author Workflow]
    RUN --> WF

    WF --> EXEC[Execute Workflow]
    EXPLORE --> TEST[Create Test Case]
    WF --> TEST
    TEST --> SUITE[Run Test Suite]

    RUN --> STRESS[Run Stress Test]
    WF --> STRESS

    EXEC --> ANALYZE[Analysis · Dashboard / Timeline]
    SUITE --> ANALYZE
    STRESS --> ANALYZE
    RUN --> ANALYZE
    ANALYZE --> REPLAY[Drill-down · Replay]
    ANALYZE --> LOGS[Logs · diagnose]

    HOME --> EXPORT[Export / Backup]
    classDef gap fill:#EF6C00,color:#fff;
```

Cross-cutting selectors that apply to almost every flow: the **Environment ▾** and **Profile ▾**
toolbar switchers change how requests resolve variables and authenticate, without leaving the
current surface.

### Flow 1 — App launch & workspace selection

```mermaid
flowchart TD
    A([Start]) --> B{Last workspace<br/>remembered?}
    B -- Yes --> C[Reopen workspace<br/>restore layout + theme]
    B -- No --> D[Show Welcome / Recent list]
    D --> E{Choose}
    E -- New --> F[Flow 2: Create Workspace]
    E -- Open recent --> C
    E -- Open file --> G[Pick .atsdb] --> C
    C --> H([Workspace ready])
    F --> H
```

### Flow 2 — Create Workspace

```mermaid
flowchart TD
    A([Ctrl+N]) --> B[Name + location dialog]
    B --> C{Valid?}
    C -- No --> B
    C -- Yes --> D[Create empty .atsdb · seed variable scopes]
    D --> E[Open · show Welcome document]
    E --> F[["3 CTAs: Import APIs · Add Service · Open sample"]]
    F --> G([Usable workspace — target 2 inputs])
```

### Flow 3 — Import APIs (auto-detect)

```mermaid
flowchart TD
    A([Toolbar: Import]) --> B[Import Wizard]
    B --> C{Source}
    C -- Paste cURL --> D[Parse single request]
    C -- OpenAPI file --> E[Parse OpenAPI 2/3/3.1]
    C -- Base URL --> F[Probe: openapi.json → swagger.json → /scalar]
    C -- Postman --> G[Parse collection]
    D --> H[Preview detected Services + Endpoints]
    E --> H
    F -- found --> H
    F -- none --> F1[[Error state: no spec found · suggest manual]]
    G --> H
    H --> I{Merge?}
    I -- Cancel --> Z([No change])
    I -- Merge --> J[Catalog merge → Explorer populates]
    J --> K([Endpoints ready])
```

### Flow 4 — Run a Request

```mermaid
flowchart TD
    A([Double-click endpoint]) --> B[Runner opens/focuses · defaults prefilled]
    B --> C[Adjust query / headers / body / auth]
    C --> D{Send · Ctrl+Enter}
    D --> E[Resolve variables · apply Profile auth]
    E --> F[Execute async · cancellable]
    F -- success --> G[Response + timing · recorded to History]
    F -- error/timeout --> H[[Error state: message + retry · detail to Logs]]
    G --> I{Next}
    I -- Replay --> F
    I -- Save as Endpoint --> J[Persist to a Service]
    I -- Duplicate --> C
```

### Flow 5 — Create Workflow

```mermaid
flowchart TD
    A([Workflows panel: New]) --> B[Empty Designer tab · document.workflow.id]
    B --> C[Drag endpoints from Explorer → Api nodes]
    C --> D[Add nodes from palette:<br/>Condition · Loop · Delay · Parallel · Assertion]
    D --> E[Connect ports]
    E --> F{ConnectorValidator}
    F -- invalid --> F1[[Reject: self/duplicate/wrong-direction · visible]]
    F -- valid --> G[Edge created]
    G --> H[Click node → edit config in right Inspector]
    H --> I([Workflow ready · undo/redo available])
```

### Flow 6 — Execute Workflow

```mermaid
flowchart TD
    A([Designer: F5 / Run]) --> B[Engine walks graph from entry node]
    B --> C[Dispatch node → handler · resolve timeout]
    C --> D{Node result}
    D -- Passed --> E[Live status: green + label]
    D -- Failed --> F{Failure policy}
    F -- Stop --> G[[Halt · mark failed node icon+label]]
    F -- Continue --> E
    E --> H{More nodes?}
    H -- Yes --> C
    H -- No --> I[WorkflowRunResult persisted as run tree]
    G --> I
    I --> J([Open in Timeline for drill-down])
```

### Flow 7 — Create Test Case & run a suite

```mermaid
flowchart TD
    A([Endpoint/Workflow context menu:<br/>Add Test Case]) --> B[Target prefilled<br/>endpoint OR workflow]
    B --> C[Add assertions · type list from IAssertion plugins]
    C --> D[Save to Test Suite]
    D --> E{Run}
    E -- Single case --> F[Execute · evaluate assertions]
    E -- Whole suite --> G[Run each case · progress per case]
    F --> H[Test Results pass/fail tree]
    G --> H
    H --> I([Report · aggregate pass/fail])
```

### Flow 8 — Run Stress Test

```mermaid
flowchart TD
    A([Endpoint/Workflow: Stress]) --> B[Stress Runner tab]
    B --> C[Pick mode:<br/>Sequential · Loop · Concurrent]
    C --> D[Set counts · timeout · delay]
    D --> E{Start}
    E --> F[Drive load async · stream StressProgress]
    F --> G[Live charts: P95/P99 · TPS/RPS · failure rate]
    G --> H{Done / Cancel}
    H --> I[Results persisted · not replayable]
    I --> J([Compare in Dashboard])
```

### Flow 9 — Analyze Dashboard → drill → replay

```mermaid
flowchart TD
    A([Open Dashboard]) --> B[Set time window + environment filter]
    B --> C[KPIs · success/failure donut · slowest/most-called]
    C --> D[Live refresh as runs complete]
    D --> E{Investigate a point}
    E -- Click --> F[Jump to Timeline · run drill-down]
    F --> G{Replay?}
    G -- Request run --> H[Re-execute request snapshot]
    G -- Workflow run --> I[Re-run workflow]
    G -- Stress run --> J[[Not replayable — view only]]
    H --> K([Compare before/after])
    I --> K
```

### Flow 10 — Export / Backup / Restore

```mermaid
flowchart TD
    A([File: Export / Backup]) --> B[Checkpoint DB · VACUUM INTO copy]
    B --> C[Write .apistudio ZIP<br/>manifest + database.sqlite + attachments/]
    C --> D{Secrets}
    D -- Same machine/user --> E[Round-trip via DPAPI]
    D -- Different machine --> F[[Flag SecretsNeedReprompt · list profiles/vars]]
    E --> G([Package written])
    F --> G
    G --> H{Restore later}
    H --> I[Unpack · verify DB opens]
    I --> J([Workspace reproduced])
```

### Flow 11 — Cross-cutting: Environment switch & Profile "Run As"

```mermaid
flowchart LR
    A([Toolbar: Environment ▾]) --> B[Set active environment]
    B --> C[Re-resolve env-scoped variables]
    C --> D[Subsequent requests/workflows use new values]
    E([Toolbar: Profile ▾]) --> F[Select role]
    F --> G[AuthApplicator swaps credentials per request/node]
    G --> D
```

### Flow 12 — Command Palette & Quick-Open (gap)

```mermaid
flowchart TD
    A{Keyboard} -- "Ctrl+Shift+P" --> B[Command Palette · fuzzy commands]
    A -- "Ctrl+P" --> C[Quick-Open · endpoints/workflows/tests by name]
    B --> D[Run command · e.g. Send, Run Workflow, Switch Env]
    C --> E[Open target in its document pane]
    D --> Z([Action executed])
    E --> Z
```

### Flow error/empty-state coverage

Every data surface defines the four states from `UI_BENCHMARK.md` (Empty · Loading · Error ·
Populated). In flow terms: an **empty** Explorer routes to Flow 3 (Import) or "Add Service"; a
**loading** request shows progress + cancel (Flow 4); an **error** surfaces inline with the next
action and pushes detail to the Logs panel; **populated** is the normal dense view.

---

## See also

- `UI_BENCHMARK.md` — UX foundation, dock layout, feature mapping, Top-20 improvements (the *why*).
- `UI_GUIDELINES.md` — WPF/MVVM/XAML rules (the *how*).
- `UI/DockLayout.md` · `UI/Explorer.md` · `UI/Runner.md` · `UI/WorkflowDesigner.md` ·
  `UI/Dashboard.md` · `UI/ProfilesAndEnvironments.md` — module implementation specs.
- `FEATURES/*` — feature-level behavior (Import, Workflow, TestCases, StressTesting, Logging,
  Packaging, Profiles, Environments, Variables, Runner, Dashboard).
- `CLAUDE.md` — project constitution and domain model.
