# Deliverable 11 — Pain Point Backlog

> Step 8 — a structured, actionable backlog. Every item has Title, Description, Severity, Frequency,
> Affected Journey, Impact, Recommendation, Priority. Grouped into the 9 required categories.
> Evidence traces to Deliverables 3/4/5/6/7/8. **No fixes are applied here** — this is the plan.

## Legend

- **Severity** (S1–S4): see `00-README.md`.
- **Frequency:** Constant (every session) · Frequent (most sessions) · Occasional · Rare.
- **Priority:** **P0** fix first (correctness/trust or blocks a core journey) · **P1** high
  (disconnected differentiator) · **P2** medium · **P3** polish.

## Priority roll-up (57 items)

| Priority | Count | Theme |
|---|---|---|
| **P0** | 6 | Silent-failure / trust + the broken headline journeys |
| **P1** | 14 | Disconnected capabilities + first-mile + discovery |
| **P2** | 22 | Feedback, integration hand-offs, efficiency |
| **P3** | 15 | Polish, accessibility tail, consistency |

> **✅ Resolved in Sprint 16 (Consolidation Phase 1, 2026-07-22):** PP-W1, PP-I2, PP-U1 (variable
> context seeded on all engine paths; unresolved tokens warn inline + Logs), PP-I1, PP-D3 (Profile
> "Run As" — toolbar switcher + Runner + workflow node), PP-U2 (Send Cancel), PP-U3 + PP-C5 (Save
> message honest about autosave; About shows the real version), PP-D1 (real first-run Welcome with
> CTAs), PP-U6, PP-D6 (new workspaces seeded with a default environment + `baseUrl`; a programmatic
> sample workspace backs "Open sample"), PP-A3 (automation names on the touched icon buttons + new
> controls). The three broken headline journeys (J1 Onboard, J3 Role-test, J4 Automate) are now
> demoable. Remaining items stay as scoped for Sprint 17+.

---

## 1 · Navigation

| ID | Title | Sev | Freq | Affected journey | Impact | Recommendation | Prio |
|---|---|---|---|---|---|---|---|
| PP-N1 | No primary navigation tier | S2 | Constant | All (J1–J9) | Every area buried in one View menu; product breadth invisible; no fast switch | Add Activity Rail (`Ctrl+1..5`) + Command Palette (`Ctrl+Shift+P`) + Quick-Open (`Ctrl+P`) | P1 |
| PP-N2 | Test Cases panel is a dead-end | S2 | Occasional | J5 Verify | Close it once → unreachable except Reset Layout (`MainMenuViewModel.cs:52-58`) | Add Test Cases to View menu + rail; guard against orphaning | P1 |
| PP-N3 | Last workspace not auto-reopened | S3 | Constant | J1/J-open | Daily users re-pick workspace every launch | Remember + optionally auto-reopen last workspace | P2 |
| PP-N4 | Document commands unguarded | S3 | Occasional | J7 Analyze | Dashboard/Timeline/Stress open with no workspace → empty/confusing | Add `CanExecute = IsWorkspaceOpen` (`ShellViewModel.cs:413,426,439`) | P2 |
| PP-N5 | No global search / go-to-anything | S3 | Frequent | All | Tree-hunting scales badly with catalog size | Quick-Open covers this (see PP-N1) | P2 |

## 2 · Workflow (automation efficiency)

| ID | Title | Sev | Freq | Affected journey | Impact | Recommendation | Prio |
|---|---|---|---|---|---|---|---|
| PP-W1 | Workflows run with empty variable context (silent) | **S1** | Frequent | **J4 Automate** | `{{vars}}`→empty string, no warning; runs look successful but are hollow (`WorkflowEditorViewModel.cs:351-353`) | Seed `IVariableScopeSeeder` on all engine callers; warn on unresolved tokens | **P0** |
| PP-W2 | Node data-mapping is inert | S2 | Frequent | J4 | No data flows between nodes; engine ignores `edge.Mapping` (`WorkflowEngine.cs:203-225`); no authoring UI | Implement drag output→input mapping end-to-end (UI + engine) | P1 |
| PP-W3 | Switch & Variable nodes unusable | S3 | Occasional | J4 | Enum only, no handler/DI/palette (`DomainEnums.cs:115-116`) | Implement handlers, or hide the enum values | P2 |
| PP-W4 | No "Open in Timeline" after a run | S3 | Frequent | J4→J7 | Documented hand-off missing; only status-bar text | Add Open-in-Timeline button/inline run tree in designer | P2 |
| PP-W5 | No `F5` to run; no dirty indicator | S3 | Frequent | J4 | Keyboard-first workflow broken; silent unsaved edits | Add `F5`; show dirty asterisk on tab | P2 |
| PP-W6 | No failure-policy / timeout UI | S3 | Occasional | J4 | Engine supports stop/continue + timeout; user can't choose | Add run-options control (engine already supports) | P2 |

## 3 · Discoverability

| ID | Title | Sev | Freq | Affected journey | Impact | Recommendation | Prio |
|---|---|---|---|---|---|---|---|
| PP-D1 | Placeholder Welcome / no onboarding / no CTAs | S2 | Constant (first run) | **J1 Onboard** | Primary first impression is stale Sprint-04 copy; no next step (`WelcomeDocumentViewModel.cs:21-23`) | State-aware Welcome: product blurb + 3 CTAs (Import · Add Service · Open sample) | **P0** |
| PP-D2 | No Settings screen (hidden in Backup) | S2 | Frequent | J9 Configure | Preferences unreachable where users look | Dedicated searchable Settings screen | P1 |
| PP-D3 | Profile / "Run As" invisible | S2 | Frequent | J3 Role-test | No switcher, no field → differentiator hidden | Toolbar Profile ▾ + per-request/node profile field | P1 |
| PP-D4 | No context-menu launch for Test/Stress | S2 | Frequent | J5/J6 | Force reselecting a target already selected | Add "Add Test Case" / "Stress this" on endpoint & workflow | P1 |
| PP-D5 | Import not on toolbar | S3 | Frequent (early) | J1/J2 | The primary "get data in" action is buried in File | Add Import to the toolbar | P2 |
| PP-D6 | No sample workspace | S2 | Constant (first run) | J1 | "Open sample" CTA has nothing to open | Ship a small sample `.atsdb` + wire the CTA | P1 |

## 4 · Layout

| ID | Title | Sev | Freq | Affected journey | Impact | Recommendation | Prio |
|---|---|---|---|---|---|---|---|
| PP-L1 | Layout/theme global, not per-workspace | S3 | Occasional | J-open | Content is per-workspace but arrangement isn't; jarring when juggling workspaces | Scope layout per workspace (keep a global default) | P2 |
| PP-L2 | Right-panel Inspector only in Designer | S3 | Frequent | J2/J5 | "detail lives right" not a product-wide convention | Generalize inspector to Runner/Testing | P2 |
| PP-L3 | No named layout presets | S4 | Occasional | J7 | No fast "Design/Run/Analyse" context switch | Add layout presets | P3 |

## 5 · Feature Integration

| ID | Title | Sev | Freq | Affected journey | Impact | Recommendation | Prio |
|---|---|---|---|---|---|---|---|
| PP-I1 | "Run As" profile auth unreachable from UI | **S1** | Frequent | **J3 Role-test** | Engine applies `IAuthApplicator` but no UI sets `ProfileId`; headline differentiator = dead code | Profile picker on request + Api node; wire `profileId` through | **P0** |
| PP-I2 | Environment doesn't reach workflow/test/stress | **S1** | Frequent | J4/J5/J6 | Only the Runner seeds vars; the "active env" selector is a half-truth | Same fix as PP-W1 (seed all engine paths) | **P0** |
| PP-I3 | Dashboard: no filters + no drill-through | S2 | Frequent | **J7 Analyze** | Analyze→drill loop broken at both filter and click (`DashboardModels.cs:49-59`) | Add time/env filters; wire chart click → Timeline | P1 |
| PP-I4 | Two parallel history stores | S3 | Constant | J2 | `RequestHistoryEntry` + `Run/RunStep` both written; only one shown | Consolidate on the unified run spine | P2 |
| PP-I5 | Scalar importer is dead code | S3 | Rare | J-import | Shipped/documented plugin never runs (`ScalarImportPlugin.cs:37`) | Route detection to it or fold into OpenAPI + drop claim | P2 |
| PP-I6 | Attachments has no UI | S3 | Occasional | J-attach | Plumbing exists; zero UI references | Build attach/view surface (endpoint/run/test) | P2 |
| PP-I7 | Runner: no Save-as-Endpoint / Test Case | S3 | Frequent | J2→J5 | Ad-hoc exploration can't be captured | Add save-as commands from request/history | P2 |
| PP-I8 | Test results: no export (JUnit/HTML) | S3 | Occasional | J5 | Blocks CI/reporting use | Add JUnit + HTML report export | P2 |
| PP-I9 | Timeline run → source navigation missing | S3 | Occasional | J7 | Can't jump from a run back to its endpoint/workflow | Add "Open source" on a run | P2 |

## 6 · Performance

> **Net strength.** Threading is clean (96 async, no sync-over-async), large-catalog virtualization
> was extended in Sprint 14, packages are VACUUM-compacted. Only minor items:

| ID | Title | Sev | Freq | Affected journey | Impact | Recommendation | Prio |
|---|---|---|---|---|---|---|---|
| PP-P1 | Export/import: no progress bar for large workspaces | S3 | Occasional | J8 | Only status-line + final size; large packages feel frozen | Determinate progress on pack/unpack | P2 |
| PP-P2 | No measured perf baseline surfaced | S4 | Rare | — | Sprint-14 claims improvements but no numbers in-repo | Record a baseline benchmark doc | P3 |

## 7 · Usability

| ID | Title | Sev | Freq | Affected journey | Impact | Recommendation | Prio |
|---|---|---|---|---|---|---|---|
| PP-U1 | Silent failures on the unhappy path | **S1** | Frequent | J4/J-import | Empty vars, dropped bodies, no-op save — all silent; violates own doctrine | Make every failure loud (inline error + Logs); no silent empties | **P0** |
| PP-U2 | Cancel button can't cancel | **S1** | Frequent | J2 | Bound `SendCancelCommand` doesn't exist (`ApiRunnerViewModel.cs:79`); user believes they can abort | Add `IncludeCancelCommand`; wire true cancellation | **P0** |
| PP-U3 | `SaveWorkspace` no-op reports success | S2 | Frequent | J-all | Misleading "Workspace saved." (`ShellViewModel.cs:377-381`) | Make Save persist, or remove it and clarify autosave | P1 |
| PP-U4 | Missing empty/loading states across surfaces | S2 | Constant | All | 2/9 empty states, 1 loading indicator; long ops give no feedback | Add the 4-state pattern to every data surface | P1 |
| PP-U5 | Monaco/WebView2 has no fallback | S2 | Occasional | J2 | Blank response body if runtime absent; undercuts "100% offline" (`MonacoEditorView.xaml.cs:49-53`) | Plain-`TextBox` fallback at the WPF layer | P1 |
| PP-U6 | New workspace not seeded | S2 | Constant (first run) | J1 | Empty env/scopes → imports/workflows have nothing to resolve against | Seed default environment + variable scopes on create | P1 |
| PP-U7 | No `Ctrl+Enter` to send | S3 | Frequent | J2 | Hot path is button-only | Add `Ctrl+Enter` | P2 |
| PP-U8 | No `F2` inline rename in trees | S3 | Frequent | J-browse | Modal round-trip for a trivial edit | Add `F2` inline rename | P2 |
| PP-U9 | Imported query params bypass the Params grid | S3 | Occasional | J2 | Mapper's query work invisible in the editable UI (`RequestBuilderViewModel.cs:70-71`) | Hydrate the grid from imported query params | P2 |
| PP-U10 | Secret re-prompt is notice-only | S3 | Rare | J8 | No guided re-entry after cross-machine restore | Add a re-entry flow listing affected secrets | P2 |
| PP-U11 | Postman non-`raw` bodies dropped silently | S3 | Occasional | J-import | Data loss with no warning | Warn on dropped body modes; support formdata/urlencoded | P2 |
| PP-U12 | Assertion node opens with null config | S4 | Occasional | J4 | Inspector empty until first edit (`NodeViewModelFactory:52-60`) | Provide a default Assertion config | P3 |

## 8 · Accessibility

| ID | Title | Sev | Freq | Affected journey | Impact | Recommendation | Prio |
|---|---|---|---|---|---|---|---|
| PP-A1 | Chart series/axes unnamed | S3 | Occasional | J6/J7 | Screen readers can't describe charts | Add automation names to LiveCharts2 series/axes | P2 |
| PP-A2 | No custom focus-visual; WCAG-AA unverified | S3 | Frequent | All | Keyboard focus hard to track; contrast unaudited in both themes | Add focus visuals; run a WCAG-AA contrast audit | P2 |
| PP-A3 | One unlabeled icon button (`✕`) | S4 | Rare | J4 | `WorkflowEditorView.xaml:182` lacks `AutomationProperties.Name` | Add the name | P3 |

## 9 · Consistency

| ID | Title | Sev | Freq | Affected journey | Impact | Recommendation | Prio |
|---|---|---|---|---|---|---|---|
| PP-C1 | Behavioral inconsistency (verbs differ by surface) | S2 | Constant | All | Env applies to requests not workflows; Save lies; Cancel can't; Replay enabled when unsupported | One meaning per verb everywhere (root cause of "feels modular") | P1 |
| PP-C2 | Type ramp not applied to section headers | S3 | Constant | All | Headings are ad-hoc bold 14px; hierarchy reads flat | Add a shared `Title`/`Header` TextBlock style; apply it | P2 |
| PP-C3 | No shared input/grid control styles | S3 | Frequent | All | `Controls.xaml` is thin; per-view drift risk | Add shared TextBox/ComboBox/DataGrid styles | P2 |
| PP-C4 | Timeline Replay not disabled for stress | S3 | Occasional | J7 | Dead action → status-bar error (`TimelineViewModel.cs:115`) | Gate `CanReplay` on run type | P2 |
| PP-C5 | "About" says "Sprint 04 shell" | S4 | Rare | — | Stale/unprofessional (`ShellViewModel.cs:453`) | Update About | P3 |
| PP-C6 | 2 residual hardcoded margins | S4 | Rare | — | `ServiceExplorerView.xaml:99`, `StressRunnerView.xaml:6` | Move to spacing tokens | P3 |

---

## The P0 set (fix-first — correctness, trust, and the broken headline journeys)

1. **PP-W1 / PP-I2** — seed workflow (and test/stress/replay) runs with the variable context.
2. **PP-I1** — surface Profile "Run As" so role/permission testing is usable.
3. **PP-U1** — make silent failures loud (the trust issue for a QA tool).
4. **PP-U2** — fix the Cancel button.
5. **PP-D1** — replace the placeholder Welcome with a real first-run screen.

These six items convert the three Broken headline journeys (J1 onboard, J3 role-test, J4 automate)
into working ones and remove the two most dangerous trust defects. They are the spine of the
Consolidation Roadmap and the recommended Sprint 16.
