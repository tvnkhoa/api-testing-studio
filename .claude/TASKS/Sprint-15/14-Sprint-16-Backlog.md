# Deliverable 14 — Recommended Sprint 16 Backlog

> The concrete next sprint. Scoped to **Phase 1 of the Consolidation Roadmap** (critical trust
> fixes) **plus** the Executable Scenarios MVP that guards it. Deliberately *not* the whole roadmap
> — Sprint 16 should prove the consolidation thesis on the three broken headline journeys, with a
> regression net, before taking on breadth in Sprint 17+.

## Sprint 16 theme

> **"Make the results trustworthy and the headline journeys real."**
> Zero net-new feature areas. Every item either surfaces a capability the backend already has, makes
> a silent failure loud, or fixes the first mile — and each ships with an Executable Scenario guard.

## Goal

Convert the three **Broken** journeys — **J1 Onboard, J3 Role-test, J4 Automate** — into working,
demoable journeys, and eliminate the two most dangerous trust defects (silent variable resolution,
fake Save / dead Cancel). Exit the sprint with an ES harness that proves it stays fixed.

## Committed backlog (Phase-1 items + ES MVP)

| # | Item | Backlog ID | Est | Acceptance |
|---|---|---|---|---|
| 1 | Seed variable context on **all** engine run paths | PP-W1, PP-I2 | M | Workflow/test/stress/replay resolve `{{vars}}` from the active environment |
| 2 | Warn (never silently empty) on unresolved `{{tokens}}` | PP-U1 | S | Unresolved token → inline warning + Logs entry |
| 3 | Surface **Profile "Run As"** (request + Api node + toolbar ▾) | PP-I1, PP-D3 | M | A request/node runs authenticated as the selected profile |
| 4 | Fix the **Send Cancel** command | PP-U2 | S | Cancel aborts an in-flight request |
| 5 | Fix **Save** (persist or remove + clarify autosave) | PP-U3 | S | No success message for a no-op |
| 6 | Real first-run **Welcome** (blurb + 3 CTAs) | PP-D1 | S | CTAs perform Import / Add Service / Open sample |
| 7 | **Seed** new workspace (default env + scopes) + ship a **sample** | PP-U6, PP-D6 | S | New workspace is immediately usable; sample opens |
| 8 | **Executable Scenarios MVP** (runner + FlaUI provider + 8 seed scenarios + Markdown reports) | Deliverable 13 | M | The 8 seed scenarios run and produce a screenshot report |
| 9 | Add missing `AutomationId`/`Name` for the seed scenarios | PP-A3 | S | FlaUI locates every control the seeds touch |

**Estimated sprint size:** ≈ 2–2.5 weeks for one developer, or ~1.5 weeks with the ES MVP done in
parallel. All items are wiring/surfacing on existing, tested backend — **low architectural risk**.

## Explicitly out of scope for Sprint 16 (deferred, with reason)

| Deferred | To | Why |
|---|---|---|
| Command Palette / Activity Rail / Quick-Open (PP-N1) | Sprint 17 (Phase 2) | Navigation consolidation is its own theme; not a trust blocker |
| Node data-mapping, Switch/Variable nodes (PP-W2/W3) | Sprint 17–18 (Phase 3) | Largest item; needs context-seeding (item 1) landed first |
| Dashboard filters + drill-through (PP-I3) | Sprint 17 (Phase 3) | Analyze loop, not a headline-journey blocker |
| Empty/loading/error states sweep (PP-U4) | Sprint 18 (Phase 4) | Broad polish; do after features work |
| Attachments UI, Settings screen (PP-I6, PP-D2) | Sprint 17–18 | Reachability, not trust |
| Monaco fallback (PP-U5) | Sprint 17 | Real but environment-specific; not the demo path |

## Definition of Done for Sprint 16

- The three headline journeys (J1/J3/J4) complete end-to-end and are **demoable**.
- No silent failure remains on those journeys — every unhappy path is visible.
- `dotnet build ApiTestingStudio.slnx` stays **0 warnings / 0 errors**; existing 322 tests green.
- The 8 ES seed scenarios **pass** and emit a screenshot report checked into `reports/`.
- Docs updated: mark the fixed items in `Sprint-15/11-Pain-Point-Backlog.md`; correct the stale
  `ROADMAP.md` note (OpenAPI mapping, Scalar status) and the "Sprint 04" About string.

## Success metric

Re-scoring after Sprint 16 should move the composite **Product Completion from ≈67% toward ≈78%**
(Reachable 70→~82%, Integrated 48→~58%) and lift **UX Error-handling from 3 → ~6** — driven almost
entirely by items 1–5. That is the proof that *consolidation, not construction* is the right strategy
for this product.

---

## One-paragraph pitch (for the sprint kickoff)

> API Testing Studio has already built the hard parts — a clean, tested engine; strong secret
> storage; a unified run spine. What it lacks is the connective tissue that makes those parts a
> product. Sprint 16 doesn't add features; it *connects* them: workflows will finally use your
> environment, requests will finally authenticate as a role, failures will finally be visible, and
> new users will finally have somewhere to start — each fix guarded by a repeatable, screenshotting
> scenario. Two weeks of wiring turns three broken headline journeys into working demos.
