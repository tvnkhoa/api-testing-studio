# Deliverable 6 — UX Evaluation (scored)

> Step 5 — score every major UX category and explain every deduction. Scores are grounded in
> quantitative evidence (counts + `file:line`), not impressions. Scale: 0 (absent) → 10 (best-in-class).

## Scorecard

| # | Category | Score | One-line basis |
|---|---|:--:|---|
| 1 | Navigation | **4** | Single flat View menu; no rail/palette/quick-open; a hard dead-end (Test Cases) |
| 2 | Visual hierarchy | **6** | Tokens strong, but the Title/Headline ramp is unused in section headers (flat) |
| 3 | Information hierarchy | **6** | Dense surfaces are scannable; inspector idiom not generalized |
| 4 | Consistency | **6** | Excellent tokens/labels; damaging *behavioral* inconsistency |
| 5 | Responsiveness | **9** | 96 async methods, no sync-over-async, clean UI-thread marshalling |
| 6 | Workflow efficiency | **4** | Repeated target re-selection; no cross-feature launch; almost no shortcuts |
| 7 | Error handling | **3** | Silent failures on the unhappy path; misleading success; 1 inline-error surface |
| 8 | Loading states | **3** | Only the Import Wizard shows progress; long ops (send/run/stress) have none |
| 9 | Empty states | **3** | Only 2 of 9 data surfaces have a real empty state; none with a CTA |
| 10 | Accessibility | **7** | ~80 `AutomationProperties.Name`; minor gaps; focus visuals rely on defaults |
| 11 | Keyboard productivity | **2** | Only `Ctrl+N/O/S`; 84 commands, 3 reachable by key |
| 12 | Learnability | **4** | Familiar patterns *once found*, but no onboarding and hidden differentiators |
| 13 | Discoverability | **3** | No palette/rail/search; Settings hidden in "Backup"; features invisible |
| — | **Overall UX** | **4.7 / 10** | Strong engineering substrate; weak first-mile, feedback, and keyboard layers |

---

## Category detail & deductions

### 1. Navigation — 4/10
Every one of the 8 functional areas is reached through one nested **View menu**; there is no
Activity Rail, Command Palette, Quick-Open, or global search (all documented `(gap)`, confirmed
absent). **Test Cases has no menu entry** and becomes unreachable once closed
(`MainMenuViewModel.cs:52-58`). Dashboard/Timeline/Stress commands lack an `IsWorkspaceOpen` guard.
*Deductions:* −3 missing primary-nav tier; −2 dead-end; −1 unguarded commands.

### 2. Visual hierarchy — 6/10
Design tokens are genuinely well-adopted (see §4). **But the type ramp is applied in only ~3
places** (Welcome headline, Dashboard KPI, Stress metric); section headers ("History",
"Configuration", "Live metrics", card titles) are ad-hoc `FontWeight="Bold"` at 14px body size —
there is **no shared Title/Header style**, so the Title(20)/Headline(28) steps are effectively unused
for headings. Hierarchy therefore reads flat despite a good token system. *Deductions:* −3 flat
heading hierarchy; −1 no shared header style.

### 3. Information hierarchy — 6/10
Dense surfaces (Timeline, Logs, Dashboard) are scannable and correctly aligned. The main weakness is
the **inconsistent inspector idiom**: a right-panel Properties/Inspector exists *only* in the
Workflow Designer; the Runner's right column is a history list, and Testing has no inspector — so
"detail lives right" is not a product-wide convention. *Deductions:* −3 inspector not generalized;
−1 mixed edit paradigms (inline vs modal vs apply-immediately).

### 4. Consistency — 6/10
**Corrects a stale doc premise:** the docs claim views hardcode `#22000000`/`#33888888` and literal
margins/fonts — that has **already been remediated**. Evidence: **0 hardcoded hex in views**
(all hex lives in `Themes/Tokens.xaml`), **0 literal `FontSize`**, only **2 residual literal
margins** (`ServiceExplorerView.xaml:99`, `StressRunnerView.xaml:6`). Lexical consistency (canonical
labels) is also strong. The score is held down by **behavioral inconsistency**: Environment applies
to requests but silently not to workflows; "Save" reports success but is a no-op; Cancel can't
cancel; Replay is enabled where unsupported. *Deductions:* −4 behavioral inconsistency; −0 tokens
(a strength). *Note:* no shared TextBox/DataGrid/ComboBox styles (`Controls.xaml` is thin) — a minor
consistency risk going forward.

### 5. Responsiveness — 9/10
The strongest category. **96 `async Task` methods across 15 ViewModels; no sync-over-async** in the
UI or Application layers (the apparent `.Result` hits are enum/property false positives).
`DashboardViewModel` captures the `SynchronizationContext` for live updates; cancellation is wired
where implemented. *Deduction:* −1 because the *perception* of responsiveness is undercut by missing
loading indicators (§8) — the shell doesn't block, but the user can't always tell work is happening.

### 6. Workflow efficiency — 4/10
Hot paths cost more than they should: **Testing/Stress force re-selecting a target** you already had
selected (no context-menu launch), the **last workspace isn't auto-reopened**, area switching means
**hunting the View menu**, rename is a **modal round-trip** (no F2), and secrets must be
**re-entered manually** after a cross-machine restore. *Deductions:* −3 repeated actions / no
cross-feature launch; −3 almost no keyboard acceleration (see §11).

### 7. Error handling — 3/10
Violates the product's own doctrine ("honest, actionable feedback"). The unhappy path is frequently
**silent**: workflow `{{vars}}` → empty string with no warning (S-16); out-of-scope variables never
load silently (S-12); Postman non-`raw` bodies dropped silently (S-06); `SaveWorkspace` reports
success while doing nothing. Only **one surface has an inline error block** (Import Wizard,
`ImportWizardDialog.xaml:46-50`); elsewhere errors go to the status bar or nowhere. *Deductions:*
−4 silent failures; −2 misleading success; −1 single inline-error surface. **This is the most
dangerous category for a testing tool** — a tool that quietly passes hollow runs cannot be trusted.

### 8. Loading states — 3/10
**Only the Import Wizard shows progress** (`ProgressBar` on `IsBusy`). Send, workflow run, stress
run, and export show **no spinner/skeleton** — buttons toggle enabled state at most. No skeletons
anywhere. *Deductions:* −4 no progress on primary long ops; −3 no determinate progress where length
is known (import is the lone exception).

### 9. Empty states — 3/10
**Only 2 of 9 data surfaces** define a real empty state (Timeline "No runs recorded yet.", LogViewer
"No log events match…"), and **neither offers a CTA**. Explorer, Runner, Dashboard, Test Results,
Stress, and the Workflows panel show a bare grid/tree. The Welcome document *is* the first-run empty
state and it's a placeholder (Deliverable 2). *Deductions:* −4 most surfaces lack empty states; −3
no empty-state CTAs (the benchmark's Bruno/Docker/GitHub-Desktop pattern).

### 10. Accessibility — 7/10
Unusually thorough for a WPF app: **~80 `AutomationProperties.Name`** across ~20 views; icon-only
Explorer buttons carry both tooltip and automation name; Dashboard widgets and the workflow canvas
are named. *Deductions:* −1 one unlabeled icon button (`WorkflowEditorView.xaml:182` `✕`); −1 chart
series/axes unnamed; −1 no custom focus-visual style and WCAG-AA contrast unverified in both themes
(a stated standard, not yet audited).

### 11. Keyboard productivity — 2/10
The whole application has **3 keyboard bindings** — `Ctrl+N/O/S` in `ShellWindow.xaml:11-13`. There
are **84 `[RelayCommand]`s but only 3 are reachable by key.** Missing: `Ctrl+Enter` (send), `F5`
(run), `F2` (rename), `Ctrl+P`/`Ctrl+Shift+P` (no palette exists), `Ctrl+1..5`. For a keyboard-first
developer/QA tool this is a major shortfall. *Deductions:* −6 hot paths have no shortcut; −2 no
palette/quick-open.

### 12. Learnability — 4/10
Once a user finds a feature, the patterns are familiar (Rider-like tree, Postman-like runner, n8n-like
canvas) — that's worth points. But there is **no onboarding, no product tour, no sample workspace**,
and the differentiators (profiles, workflow variables) are hidden or silently inert, so the mental
model a user builds is *incomplete and partly wrong* (they may believe workflows use their
environment). *Deductions:* −3 no onboarding/sample; −3 hidden/inert differentiators mislead the model.

### 13. Discoverability — 3/10
No command palette, no quick-open, no global search, no activity rail; Settings hidden inside a
"Backup" dialog; role/profile testing invisible; Testing/Stress not launchable from their targets;
Attachments has no UI at all. The product's real breadth is **invisible**. *Deductions:* −4 no
discovery surfaces; −3 hidden features/settings.

---

## What the scores say

- **Strengths to protect (8–9):** responsiveness/threading, token discipline, accessibility coverage.
  These are hard to build and already done well.
- **The 2–3 band is the consolidation target:** keyboard, empty/loading states, error handling,
  discoverability. These are **cheap to fix and disproportionately shape "professional feel."**
- **Error handling at 3 is the priority deduction** — for a QA/testing product, silent failures are a
  correctness/trust issue, not just polish.

**Overall UX: 4.7/10** — a professional-grade engineering substrate wearing an under-finished
interaction layer. The gap between the two is the whole point of the consolidation sprint.
