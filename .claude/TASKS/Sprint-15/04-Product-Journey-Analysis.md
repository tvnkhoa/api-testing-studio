# Deliverable 4 — Product Journey Analysis

> Step 3 — Product Journey Review. Evaluating **complete workflows end-to-end**, not screens in
> isolation. Where does a real journey stall, dead-end, repeat work, hide a step, or fail silently?
> Grounded in the scenario catalog (Deliverable 3) and the four code traces behind it.

## How to read this

Each journey is scored **Intact / Degraded / Broken** on whether a motivated user can complete it
*and trust the result*. The recurring cross-cutting patterns are called out first, because they
explain most of the individual failures.

---

## The five cross-cutting patterns (the real story)

### Pattern A — Capability without UI hand-off *(the dominant theme)*
The Application/Domain layers repeatedly implement a capability that the UI never exposes. The
feature is *built and tested* but *unreachable*:

| Built capability | Missing UI | Consequence |
|---|---|---|
| `AuthApplicator` + `RequestNodeHandler` ProfileId | No profile picker / switcher (S-14) | Role testing = dead code |
| `IVariableScopeSeeder` | Never called on workflow paths (S-16) | Workflows ignore all variables/envs |
| `WorkflowEdge.Mapping` | No authoring UI + engine ignores it (S-27) | No data flow between nodes |
| `DashboardQuery.Source` | Not surfaced; no time/env fields (S-18) | Dashboard can't be filtered/drilled |
| Stress P95/P99/RPS metrics | No charts (S-17) | "Visual stress testing" unrealized |
| `AttachmentStore` | Zero UI references (S-25) | Attachments unusable |
| `WorkflowRunOptions.FailurePolicy`/timeout | No run-options UI (S-16) | Users can't choose stop/continue |

**Implication:** consolidation is mostly *wiring and surfacing*, not building. High leverage, low risk.

### Pattern B — Silent failure (no feedback on the unhappy path)
The product frequently does the wrong thing *quietly*:
- Workflow `{{vars}}` → **empty string, no warning** (S-16) — worst offender; results look real but aren't.
- Variables in Workflow/Local/WorkflowOutput scopes **never load, no feedback** (S-12).
- Postman non-`raw` bodies **silently dropped** (S-06).
- `SaveWorkspace` is a **no-op that reports "Workspace saved."** (`ShellViewModel.cs:377-381`).
- Cancel button that **cannot cancel** (S-09).
- Timeline Replay on a stress run → **status-bar error instead of a disabled button** (S-19).

**Implication:** the product violates its own doctrine ("honest, actionable feedback"; "state with
icon+text"). Trust is the casualty — a QA tool that silently passes empty requests is dangerous.

### Pattern C — Discoverability dead-ends
- **No Activity Rail / Command Palette / Quick-Open / global search** — everything is one nested
  View menu (S-26).
- **Test Cases panel unreachable once closed** (no menu entry) (S-24/S-26).
- **Settings hidden inside a "Backup" dialog** (S-21).
- **No context menus to launch Testing/Stress** from an endpoint/workflow (S-17/S-24).
- **Profile/role concept invisible** — no switcher, no field (S-14).

**Implication:** the product's breadth is real but *invisible*. New users can't find the differentiators.

### Pattern D — Onboarding vacuum
Placeholder Welcome (S-01) + empty un-seeded workspace (S-02) + no sample + no CTAs = a first mile
that actively works against activation. Everything downstream assumes populated state the product
never helps you reach.

### Pattern E — Fragmented / duplicated foundations
- **Two history systems** written on every send (`RequestHistoryEntry` *and* `Run/RunStep`), only
  one shown (S-11).
- **Scalar importer is dead code**; Scalar works by accident through OpenAPI (S-05).
- **Global (not per-workspace) layout/theme** while content is per-workspace (S-03) — a scoping
  mismatch users will feel when juggling workspaces.

---

## Journey-by-journey verdict

### J1 · "I'm new — get me productive" — **Broken**
Launch → placeholder Welcome (no idea what the tool is) → `Ctrl+N` → empty workspace, empty tree,
no CTAs → dead stop. Recovery requires *knowing* to use Import. The intended "2 inputs to usable"
journey does not exist. **Root:** Patterns C + D.

### J2 · "Import an API and call an endpoint" — **Degraded (works, with a trap)**
Import (OpenAPI/Postman) → populated tree → double-click endpoint → Runner → Send → response.
This core loop **works** and feels competent. Degradations: no `Ctrl+Enter`, dead Cancel, **no way
to authenticate via a Profile** (so any secured API can't be called as a role), imported query
params invisible in the grid, and a **blank body if WebView2 runtime is missing**. **Root:** Patterns A + B.

### J3 · "Test a secured endpoint as a specific role" — **Broken**
The product's stated differentiator. The engine can do it; the UI offers no profile selection
anywhere (S-14). A user literally cannot exercise role/permission testing. **Root:** Pattern A.

### J4 · "Automate a multi-step workflow with real data" — **Broken (silently)**
Build workflow (drag endpoints — nice) → set environment → Run. Per-node status animates and it
*looks* successful, but every `{{var}}` resolved to empty (S-16) and **no data flows between nodes**
(S-27, no mapping). The signature "workflow-first" journey produces confident-looking but hollow
runs, with Switch/Variable nodes absent. **Root:** Patterns A + B.

### J5 · "Verify behavior with test cases" — **Degraded**
Assertions (plugin-driven) and pass/fail tree work. But you can't launch a test from the endpoint
you're looking at (no context menu, target re-selected from a global combo), the panel is a
dead-end if closed, and there's no exportable report for CI. **Root:** Patterns A + C.

### J6 · "Load-test an endpoint" — **Degraded**
Modes, cancellation, live metrics, persistence all work — but it's **numbers, not charts**, and
there's no "stress this endpoint" gesture. Functional, but doesn't deliver the promised visual punch.
**Root:** Patterns A + C.

### J7 · "Analyze activity, then investigate a problem" — **Broken at the drill**
Dashboard shows live KPIs — good. But you **can't filter by time/env** and **clicking a chart does
nothing**, so the analyze→drill→replay loop stops at the first click. Timeline itself (reached
separately via the menu) *does* drill and replay well. The pieces exist; the bridge doesn't.
**Root:** Patterns A + C.

### J8 · "Move my workspace to another machine" — **Intact (honest)**
Export → `.apistudio` → Import elsewhere works and is **honest about secrets** (re-prompt flag).
The only gap is that the re-prompt is a notice with no guided re-entry, and export feedback is thin.
This is the most complete end-to-end journey in the product. **Root:** minor (Pattern B tail).

### J9 · "Configure the tool to my liking" — **Broken (no destination)**
There is nowhere to go: no Settings screen; preferences live under "Backup." **Root:** Patterns C.

---

## Repeated actions / too many clicks (efficiency)

- **Re-selecting a target** you already had selected, in Testing and Stress (no context launch).
- **Re-opening the last workspace** every session (no restore-last).
- **Hunting the View menu** for every area switch (no rail/palette/shortcuts).
- **Round-tripping a dialog to rename** in the Explorer (no F2).
- **Re-entering secrets manually** after a cross-machine restore (notice-only).

## Unclear terminology / consistency

Terminology in the code largely honors the canonical labels in `UI_INFORMATION_ARCHITECTURE.md`
(Workspace/Service/Endpoint/Profile/Environment/Variable/Workflow/Node/Run). The confusion is
**conceptual, not lexical**: Environment appears global (toolbar) but silently doesn't apply to
workflows; "Save" implies persistence but is a no-op; a Profile is a first-class domain concept with
no visible home. These are *behavioral* inconsistencies, more damaging than naming ones.

## "One product or a collection of modules?" (preview of Deliverable 5)

**Today it reads as a collection of strong, independently-built modules sharing a shell.** The
evidence is the systematic absence of *connective tissue*: modules don't launch each other
(endpoint→test/stress), don't share context (environment→workflow, profile→request), and don't hand
off (dashboard→timeline, workflow→timeline). The unified `Run/RunStep` store is the one genuine
integrating spine — and it's the healthiest part of the product. Consolidation should treat that
run spine as the model for wiring the rest together. Full IA verdict in Deliverable 5.

## What is genuinely good (protect these during consolidation)

- **Clean build, 322 tests, strong Application/Domain layer** — the hard part is done.
- **Service Explorer + API Runner core loop** — familiar, competent, fast.
- **Import wizard** with auto-detect + preview + merge.
- **Secret storage** — AES-256-GCM + DPAPI master key; better than advertised.
- **Unified Run/RunStep tree** feeding Timeline/Dashboard/Replay — the right architecture.
- **Message-driven document opening** in the shell — correct, IDE-like foundation.
- **Honest cross-machine export** with secret re-prompt flagging.

---

## Phase 1 conclusion

The product does not have a *quality* problem; it has a **connection, feedback, and first-mile**
problem. The highest-impact, lowest-risk consolidation work is to (1) surface capabilities the
backend already has (Pattern A), (2) make failures loud (Pattern B), (3) make breadth discoverable
(Pattern C), and (4) fix the first mile (Pattern D). Phase 2 will quantify this via the IA review,
scored UX evaluation, feature-relationship matrix, and per-sprint verification; Phase 3 turns it
into the ranked backlog and roadmap.
