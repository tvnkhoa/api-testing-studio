# Deliverable 7 — Feature Relationship Matrix

> Step 6 — map how features relate: which belong together, which should launch each other, which
> should share context, and which are isolated today. Recommend integration points (no redesign).

## The core insight

Every feature is built around **one shared spine**: the unified `Run / RunStep` tree, which already
integrates Runner, Workflow, Stress, Timeline, Dashboard, and Replay. That spine proves the
architecture *can* be one product. The missing relationships are almost all in the **UI launch and
context-sharing layer**, not the data layer.

Three relationship types are evaluated:
- **Launch** — feature A opens feature B with context prefilled (right-click gestures, buttons).
- **Context** — feature A's state (active Environment, active Profile, selected Endpoint) flows into B.
- **Hand-off** — after A completes, the user is carried to B to continue (result → analysis).

## Relationship matrix (current state)

Legend: ✅ wired · 🟡 partial · ❌ missing · — n/a

| From ↓ / To → | Runner | Workflow | Testing | Stress | Timeline | Dashboard | Logs | Environment | Profile |
|---|---|---|---|---|---|---|---|---|---|
| **Explorer (endpoint)** | ✅ open | ✅ drag→node | ❌ launch | ❌ launch | — | — | — | — | — |
| **Runner** | — | ❌ save→node | ❌ save→test | ❌ launch | 🟡 records | 🟡 records | 🟡 errors | 🟡 uses | ❌ uses |
| **Workflow** | — | — | 🟡 target | ❌ launch | ❌ open | 🟡 records | 🟡 errors | ❌ uses | ❌ uses |
| **Testing** | — | 🟡 target | — | ❌ | 🟡 records | 🟡 records | 🟡 | ❌ uses | ❌ uses |
| **Stress** | — | 🟡 target | — | — | ✅ records | ✅ records | 🟡 | ❌ uses | ❌ uses |
| **Dashboard** | — | — | — | — | ❌ drill | — | — | ❌ filter | — |
| **Timeline** | ✅ replay | ✅ replay | — | 🟡 replay-blocked | — | — | — | — | — |

**Reading the matrix:** the data spine (records into Timeline/Dashboard) is mostly ✅/🟡; the
**launch column and the context (Environment/Profile) columns are almost entirely ❌.** That is the
quantified shape of "modules, not a product."

## A. Features that naturally belong together (and whether they're grouped)

| Cluster | Members | Grouped today? |
|---|---|---|
| **APIs** | Explorer + Runner + History | ✅ (message-driven open) |
| **Identity** | Profiles + Environments + Variables | ✅ one tabbed panel — but Profile has no switcher |
| **Analysis** | Dashboard + Timeline + Replay | 🟡 same data, two disconnected menu items, no drill link |
| **Verification** | Test Cases + Assertions + Test Results | ✅ within the panel; ❌ isolated from its targets |
| **Load** | Stress config + Live metrics + results | 🟡 works, but orphaned from endpoints/workflows |
| **Automation** | Workflow designer + engine + nodes | 🟡 built, but doesn't consume Identity context |

## B. Features that SHOULD launch each other (missing gestures)

These are the highest-leverage integration points — all are **right-click/button wiring**, not new
features:

1. **Endpoint → "Run"** (already ✅ double-click) — keep.
2. **Endpoint → "Add Test Case"** ❌ — target prefilled (today: reselect from a global combo). *(S-24)*
3. **Endpoint → "Stress this endpoint"** ❌ — prefill Stress target. *(S-17)*
4. **Endpoint → "Add to Workflow"** 🟡 — drag works, but no menu equivalent.
5. **Workflow → "Add Test Case"** 🟡 — target selectable but not launched from the workflow.
6. **Workflow → "Stress this workflow"** ❌ — prefill Stress target.
7. **Runner request → "Save as Endpoint" / "Save as Test Case"** ❌ — capture ad-hoc work. *(S-11)*
8. **Workflow run → "Open in Timeline"** ❌ — the documented hand-off isn't wired. *(S-16)*
9. **Dashboard chart point → "Open run in Timeline"** ❌ — breaks analyze→investigate. *(S-18)*
10. **Timeline run → "Open source" (endpoint/workflow)** ❌ — navigate from a run back to its origin.

## C. Features that SHOULD share context (missing flows)

The two cross-cutting selectors defined in the IA — **active Environment** and **active Profile** —
are the product's shared context. Their reach today:

| Context | Runner | Workflow | Testing | Stress | Status |
|---|---|---|---|---|---|
| **Active Environment** (variables) | ✅ seeded | ❌ never seeded | ❌ | ❌ | **Only the Runner honors it** *(S-13/S-16)* |
| **Active Profile** ("Run As" auth) | ❌ no UI | ❌ no UI | ❌ | ❌ | **Nothing honors it from the UI** *(S-14)* |

This is the deepest integration failure: the two things meant to flow *everywhere* flow *almost
nowhere*. Fixing context propagation (seed `IVariableScopeSeeder` on all engine paths; add a Profile
switcher + per-request/node ProfileId) would make Environment/Profile behave as the product-wide
selectors the IA promises.

## D. Currently isolated features (islands)

- **Test Cases** — no launch in, no menu home, no result export out. The most isolated feature.
- **Stress** — records to the spine, but no launch in and no charts; a one-way island.
- **Attachments** — fully isolated (no UI at all). *(S-25)*
- **Dashboard** — receives data but sends nothing onward (no drill, no filter). A read-only island.
- **Profiles** — a first-class domain concept with no runtime touch-point in the UI.

## E. Recommended integration points (ranked by impact/effort)

| # | Integration | Type | Impact | Effort |
|---|---|---|---|---|
| 1 | Seed Environment/Variables into **all** engine paths (workflow/test/stress/replay) | Context | **Very high** — makes automation real | S–M |
| 2 | Endpoint/Workflow **context-menu launchers** (Run / Test / Stress / Add-to-Workflow) | Launch | **Very high** — connects the modules | S–M |
| 3 | **Profile switcher** + per-request/node ProfileId | Context | **High** — unlocks role testing | M |
| 4 | **Dashboard → Timeline** click-through + **Workflow → Timeline** open | Hand-off | **High** — closes analyze loop | S–M |
| 5 | Runner **Save as Endpoint / Test Case** | Launch | Medium | S |
| 6 | **Timeline run → source** navigation | Hand-off | Medium | S |
| 7 | Consolidate the **two history stores** onto the run spine | Context | Medium (removes fragmentation) | M |
| 8 | **Attachments** surface (attach to endpoint/run/test) | Launch | Medium | M |

## Integration health score

**3.5/10.** The unifying *architecture* (shared run spine, message bus, DI) is excellent; the
*realized* cross-feature relationships are sparse — launch and context links are the missing 60%.
Because the plumbing exists, most of these are low-risk wiring tasks, which is why consolidation
(not rebuild) is the right strategy. This directly informs the Consolidation Roadmap (Deliverable 12).
