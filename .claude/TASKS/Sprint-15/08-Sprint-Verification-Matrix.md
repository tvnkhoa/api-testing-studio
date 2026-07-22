# Deliverable 8 — Sprint Verification Matrix

> Step 7 — review every completed sprint (00–14): implemented features, acceptance criteria,
> product integration, remaining gaps. Verified by spot-checking each sprint's key classes against
> the code and reconciling with the Phase-1 scenario/journey findings.

## Sprint Completion Matrix

| Sprint | Title | ACs met | Key gap(s) | Built % |
|---|---|---|---|---:|
| 00 | Architecture Validation & Spike | ✅ all | — | 100% |
| 01 | Foundation | ✅ all | — | 100% |
| 02 | Workspace Storage | ✅ all | `SaveWorkspace` is a no-op that reports success | 95% |
| 03 | Plugin Infrastructure | ✅ all | — | 100% |
| 04 | Shell UI | ✅ all | Layout global not per-workspace; nav is View-menu-only; thin create flow | 90% |
| 05 | Service Explorer | ✅ all in-scope | No F2 rename; in-tree drag-drop formally descoped | 100% |
| 06 | API Runner | 🟡 | **Cancel unmet**; Monaco no-fallback (blank body offline); cookie jar dropped | 80% |
| 07 | Import System | 🟡 | **Scalar importer dead code**; Postman env missing + lossy bodies | 80% |
| 08 | Workflow Engine (headless) | ✅ all | Engine ignores `edge.Mapping` (surfaces in S09) | 100% |
| 09 | Workflow Designer | 🟡 | **Run has empty var context (silent)**; data-mapping inert; Switch/Variable nodes unusable | 75% |
| 10 | Profiles & Environments | 🟡 | **"Run As" auth unreachable (no UI)**; env vars never reach workflows; 3 scopes no-op | 75% |
| 11 | Assertions & Test Cases | 🟡 | Panel dead-end (no menu); no prefilled target; no result export | 85% |
| 12 | Stress Runner | 🟡 | **No charts** (numeric tiles only); no context-menu launch | 90% |
| 13 | Dashboard & Logging | 🟡 | **No time/env filter; no chart→Timeline drill**; Replay not guarded for stress | 85% |
| 14 | Packaging & Polish | ✅ most | Thin export feedback; secret re-prompt notice-only; Attachments no UI | 90% |

**Legend:** ✅ = acceptance criteria functionally met · 🟡 = met at the Application/Domain layer but
with a material UI/integration gap. No sprint is ❌ (nothing was left unbuilt); the failures are
integration failures, not missing implementations.

## Delivered vs. Integrated (the key distinction)

The backend/engine work is essentially **code-complete**; the shortfall is almost entirely in **UI
wiring and cross-feature integration** — the review's Pattern A ("capability without UI hand-off").

| Bucket | Sprints | Count |
|---|---|---|
| **Fully delivered & integrated** | 00, 01, 02, 03, 05, 08 | **6** |
| **Code-complete but integration-gapped** | 04, 06, 07, 09, 10, 11, 12, 13, 14 | **9** |

> Note vs. the sub-report: Sprint 14 is placed in the integration-gapped bucket here because,
> although its packaging ACs pass, it inherits the Attachments-no-UI gap and thin feedback.

## Feature Coverage Matrix

How each shipped feature scores on **Built** (implemented + tested) vs. **Reachable** (a user can
actually get to and complete it) vs. **Integrated** (connects to the rest of the product).

| Feature | Built | Reachable | Integrated | Net |
|---|:--:|:--:|:--:|---|
| Workspace lifecycle | ✅ | ✅ | ✅ | Healthy (Save no-op aside) |
| Service Explorer / CRUD | ✅ | ✅ | ✅ | Healthy |
| Import (OpenAPI / cURL / Postman) | ✅ | ✅ | 🟡 | Postman lossy; templates not linked to Variables |
| Import (Scalar) | ✅ | ❌ | ❌ | Dead code (unreachable) |
| API Runner (send/response/timing) | ✅ | ✅ | 🟡 | No profile auth; blank body if no WebView2 |
| Request history / replay | ✅ | ✅ | 🟡 | Two stores; no save-as-endpoint |
| Profiles / secret storage | ✅ | ✅ | ❌ | No runtime touch-point ("Run As" unreachable) |
| Environments / variables | ✅ | ✅ | 🟡 | Applies to Runner only, not workflows |
| Workflow designer | ✅ | ✅ | 🟡 | No F5; no dirty indicator |
| Workflow engine (headless) | ✅ | ✅ | 🟡 | Not seeded with variables at run |
| Workflow data-mapping | 🟡 | ❌ | ❌ | Field persists; UI + engine ignore it |
| Switch / Variable nodes | ❌ | ❌ | ❌ | Enum only; no handler |
| Test cases / assertions | ✅ | 🟡 | ❌ | Panel dead-end; no launch from targets; no export |
| Stress runner | ✅ | 🟡 | 🟡 | No charts; no context launch |
| Dashboard | ✅ | ✅ | ❌ | No filters; no drill-through |
| Timeline / drill / replay | ✅ | ✅ | 🟡 | Replay not guarded for stress |
| Logs | ✅ | ✅ | ✅ | Healthy |
| Export / package | ✅ | ✅ | ✅ | Healthy (thin feedback) |
| Backup / restore | ✅ | ✅ | 🟡 | Secret re-prompt is notice-only |
| Attachments | 🟡 | ❌ | ❌ | Plumbing only; no UI |
| Settings | ❌ | ❌ | ❌ | Only a Backup dialog |
| Command palette / quick-open / rail | ❌ | ❌ | ❌ | Not built (documented gap) |

**Coverage tallies (22 features):** Built ✅ 17 · 🟡 3 · ❌ 2 — **~86% built.**
Reachable ✅ 14 · 🟡 3 · ❌ 5 — **~70% reachable.**
Integrated ✅ 6 · 🟡 9 · ❌ 7 — **~48% integrated.**

## Top 10 highest-impact unmet/partial acceptance criteria

1. **[S10 · S1] "Run As" profile auth is unreachable** — engine applies `IAuthApplicator`, but no UI sets `ProfileId`; no profile switcher. Role/permission testing can't be exercised.
2. **[S09 · S1] Workflows run with an empty variable context** — `RunAsync` called with no `WorkflowContext`; `{{var}}` silently resolves to empty. Runs look successful but are hollow.
3. **[S08/S09 · S2] Node data-mapping is inert** — engine never reads `edge.Mapping`; no authoring UI.
4. **[S09 · S2] Switch & Variable nodes unusable** — enum only, no handler/DI/palette.
5. **[S06 · S2] Cancel button cannot cancel** — no `IncludeCancelCommand`; bound command doesn't exist.
6. **[S13 · S2] Dashboard filter→drill loop broken** — no time/env fields; chart click does nothing.
7. **[S12/S13 · S2] Stress charts absent** — numeric tiles only.
8. **[S06/S10 · S2] No Monaco/WebView2 fallback** — blank response body offline undercuts "100% offline".
9. **[S07 · S3] Scalar importer dead code** — auto-detect never routes to it.
10. **[S11/nav · S2] Test Cases panel dead-end** — no menu entry, no launch from targets, no export.

The top four (S1) are exactly the Broken journeys in Deliverable 4 (J3 role testing, J4 workflow
automation). Fixing them is the substance of the Consolidation Roadmap.
