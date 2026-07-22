# Deliverable 12 — Product Consolidation Roadmap

> Step 9 — a phased roadmap, **not** an instruction to fix now. Each phase has Objectives, Expected
> User Benefit, Estimated Scope, Dependencies, and Acceptance Criteria. Sequenced so that trust and
> the broken headline journeys are restored before breadth and polish. IDs reference Deliverable 11.

## Sequencing principle

Fix **trust** before **reach**, **reach** before **discovery**, **discovery** before **polish**.
A QA tool that silently passes hollow runs (PP-W1/PP-U1) must be corrected before anything else,
because every later demo of workflows/dashboards rests on it. Effort key: **S** ≈ days ·
**M** ≈ 1–2 weeks · **L** ≈ multi-sprint.

---

## Phase 1 — Critical Product Fixes (trust & correctness)

**Objectives:** Eliminate silent failures and make the three broken headline journeys
(onboard / role-test / automate) actually work.

**Backlog items:** PP-W1, PP-I2, PP-I1, PP-U1, PP-U2, PP-U3, PP-D1, PP-U6, PP-D6.

**Expected user benefit:** Workflows honor the active environment; role/permission testing becomes
usable; failures are visible; a new user is greeted by a real first-run screen and a seeded/sample
workspace. The product becomes *trustworthy* — results mean what they appear to mean.

**Estimated scope:** M (≈1.5–2 weeks). Mostly wiring on existing backend — low risk.

**Dependencies:** None (backend capabilities already exist). Must land before Phases 3–4 demos.

**Acceptance criteria:**
- A workflow using `{{baseUrl}}` executes against the active environment's value; an unresolved
  token produces a visible warning, never a silent empty string.
- A request/Api-node can select a Profile; the request is authenticated accordingly (Bearer/Basic/ApiKey).
- The Send Cancel button aborts an in-flight request; `SaveWorkspace` either persists or is removed.
- First launch shows a product-explaining Welcome with working Import / Add Service / Open sample CTAs.
- Creating a workspace seeds a default environment + variable scopes; a sample workspace opens.

---

## Phase 2 — Navigation Consolidation (make breadth reachable)

**Objectives:** Give every functional area a memorable home and a keyboard path; remove dead-ends.

**Backlog items:** PP-N1, PP-N2, PP-N3, PP-N4, PP-D2, PP-D5, PP-U7, PP-U8, PP-W5.

**Expected user benefit:** One-key switching between areas; a command palette and quick-open;
no unreachable panels; Settings has a home; the hot paths (Send, Run, Rename) get shortcuts.

**Estimated scope:** M (≈1.5 weeks). Activity Rail + palette are the bulk.

**Dependencies:** None hard; benefits from Phase 1 (so navigated-to features actually work).

**Acceptance criteria:**
- Activity Rail switches the left panel with `Ctrl+1..5`; Test Cases is reachable from menu + rail.
- `Ctrl+Shift+P` opens a command palette listing the app's commands; `Ctrl+P` jumps to any
  endpoint/workflow/test by name.
- A dedicated Settings screen exists (theme, defaults, HTTP timeout, backup) — not buried in Backup.
- `Ctrl+Enter` sends; `F5` runs a workflow; `F2` renames in trees; document commands are workspace-gated.

---

## Phase 3 — Workflow & Feature Integration (make it one product)

**Objectives:** Wire the launch and hand-off links that connect the modules.

**Backlog items:** PP-D3, PP-D4, PP-W2, PP-W3, PP-W4, PP-I3, PP-I7, PP-I8, PP-I9, PP-C4, PP-U9, PP-U10, PP-U11.

**Expected user benefit:** Right-click an endpoint to Test/Stress/Add-to-Workflow it; workflow data
flows node→node; the Dashboard filters and drills into the Timeline; runs link back to their source;
results export for CI. The product stops feeling like separate tools.

**Estimated scope:** L (≈2–3 weeks). Node data-mapping (PP-W2) is the largest single item.

**Dependencies:** Phase 1 (context must flow before mapping/launch are meaningful); Phase 2 (a Profile
switcher home for PP-D3).

**Acceptance criteria:**
- Endpoint & workflow context menus launch Test/Stress/Add-to-Workflow with the target prefilled.
- A workflow edge can carry a data mapping authored in the UI, and the engine applies it at run time.
- The Dashboard supports a time-window + environment filter and a chart click opens the run in Timeline.
- Test/stress results export to JUnit + HTML.

---

## Phase 4 — UX & Feedback Improvements (professional feel)

**Objectives:** Complete the four-state pattern, generalize the inspector, fix consistency defects.

**Backlog items:** PP-U4, PP-U5, PP-L1, PP-L2, PP-I4, PP-I6, PP-C1, PP-C2, PP-C3, PP-P1, PP-A1, PP-A2.

**Expected user benefit:** Every surface has empty/loading/error states; the response body never
silently blanks; layout is per-workspace; the inspector is a consistent right-panel convention;
verbs behave identically everywhere; attachments are usable; charts and focus are accessible.

**Estimated scope:** L (≈2–3 weeks).

**Dependencies:** Phases 1–3 (states/inspectors should reflect the now-working features).

**Acceptance criteria:**
- All 9 data surfaces define Empty (with CTA) / Loading / Error / Populated states.
- Response body renders in a plain-text fallback when WebView2 is unavailable.
- One history store feeds the Runner; attachments can be added/viewed; a WCAG-AA contrast audit passes.

---

## Phase 5 — Visual Polish

**Objectives:** Close the cosmetic tail.

**Backlog items:** PP-L3, PP-P2, PP-U12, PP-A3, PP-C5, PP-C6.

**Expected user benefit:** Layout presets, an honest About, a perf baseline doc, and the last token
stragglers — the finishing coat.

**Estimated scope:** S (≈2–4 days).

**Dependencies:** All prior phases.

**Acceptance criteria:**
- Named layout presets; About reflects the real version; a benchmark baseline is recorded;
  zero residual hardcoded margins.

---

## Roadmap at a glance

| Phase | Theme | Items | Scope | Journeys restored |
|---|---|---|---|---|
| 1 | Critical fixes (trust) | 9 | M | J1, J3, J4 |
| 2 | Navigation consolidation | 9 | M | J9 + all (reach) |
| 3 | Feature integration | 13 | L | J5, J6, J7 |
| 4 | UX & feedback | 12 | L | quality across all |
| 5 | Visual polish | 6 | S | — |

**Total ≈ 8–10 weeks** of consolidation to take the product from **67% → ~90%** on the composite
completion measure — without adding a single net-new feature area. Phase 1 alone (≈2 weeks) moves the
needle most, because it converts the three broken headline journeys into working demos.
