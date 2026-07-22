# Deliverable 5 — Information Architecture Review

> Step 4 — evaluate the product's **structure**: navigation hierarchy, menu/toolbar/dock
> organization, feature grouping, property panels, dialogs, context menus, and consistency. The
> deciding question: does this feel like **ONE PRODUCT** or **a collection of unrelated modules**?

## Verdict up front

**It is a collection of well-built modules sharing a shell — not yet one product.** The shell,
the domain model, and the unified Run store are genuinely unifying; but the *navigation IA* is flat
and menu-bound, and the *feature IA* lacks the launch/context/hand-off links that would make the
areas feel like one tool. Score: **IA maturity 5/10** — a correct spatial skeleton (Rider/VSCode
dock model) undermined by a missing primary-navigation tier and absent cross-feature wiring.

The project's own `UI_INFORMATION_ARCHITECTURE.md` describes an excellent target IA (8 functional
areas, 3 navigation tiers, canonical labels). Most of the deductions below are the **delta between
that documented target and the shipped code**, which the docs honestly pre-label as `(gap)`.

---

## 1. Navigation hierarchy — **flat, one tier missing**

The target is three tiers (Primary = Activity Rail; Secondary = trees/lists + document tabs;
Utility = toolbar/menu/status bar/palette). **Reality ships only the Secondary and part of the
Utility tier:**

| Tier | Target | Shipped | Gap |
|---|---|---|---|
| Primary | Activity Rail (`Ctrl+1..5`) switching left panel | ❌ none | No memorable home per area |
| Secondary | Trees/lists + document tabs | ✅ Explorer/Workflows/Profiles/TestCases + AvalonDock docs | Test Cases can't be re-opened |
| Utility | Toolbar + menu + status bar + **palette/quick-open** | ✅ toolbar/menu/status bar · ❌ palette/quick-open/search | No keyboard-driven jump to anything |

**Consequence:** every one of the 8 functional areas is reached by drilling one flat **View menu**.
There is no glance-level model of the product's scope, no fast switch, no search. **S2.**

## 2. Menu organization — **the primary nav by default, and incomplete**

`ShellWindow.xaml` / `MainMenuViewModel`:
- **File:** New / Open / Open Recent / Import Package / Export Package / Backup Settings & Restore / Exit.
- **View:** Explorer · Workflows · Profiles & Environments · Logs (tool toggles) · Dashboard ·
  Timeline · Stress Runner (documents) · Dark Theme.
- **Help:** About (still reports "Sprint 04 shell").

Problems:
- **Test Cases is missing from View** entirely (`MainMenuViewModel.cs:52-58`) → the panel is a
  dead-end once closed. **S2.**
- The menu **mixes concepts**: tool-panel toggles and document-openers sit in one undifferentiated
  list, so a user can't tell "show a side panel" from "open a work surface."
- **Settings has no menu home** — it hides under File → "Backup Settings & Restore." **S2.**
- Import/Export are under **File** (fine) but the everyday **Import APIs** action is not on the
  toolbar where a new user looks first.

## 3. Toolbar organization — **under-used**

`ToolbarViewModel` exposes only **New · Open · Save · Theme · Environment ▾**. Per the benchmark the
toolbar should carry the *global, low-churn* actions — and it's missing the two most important ones:
**Import** (the primary way to get data in) and a **Profile ▾** switcher (the role-testing pillar).
There is also no **Run** control and no **search box** (both documented). The toolbar is tidy but
does too little; discoverability suffers because nothing about the product's breadth is visible here.
**S3.**

## 4. Dock layout — **correct and the strongest part of the IA**

AvalonDock with a clean tool-vs-document split, matching the documented optimal layout:
- **Tool panes:** Explorer, Workflows, Profiles, Test Cases, Logs.
- **Document panes:** Welcome, Runner (single reused), Workflow editors (one per workflow),
  Test Results, Stress, Dashboard, Timeline.
- Layout + theme persisted; message-driven open/focus/de-dupe by ContentId is implemented correctly.

Gaps: layout/theme are **global (per-user), not per-workspace** (S3 scoping mismatch — content is
per-workspace); the **right-panel Inspector convention exists only in the Workflow Designer**
(the Runner and Testing don't use a right inspector, so "detail lives right" is inconsistent — S3);
Reset Layout is the *only* recovery for the Test Cases dead-end.

## 5. Workspace organization — **coherent model, empty on arrival**

The content model is sound and consistent (Workspace → Services/Profiles/Environments/Variables/
Workflows/Test Suites/Runs/Logs/Attachments), and it matches the documented entity taxonomy. The IA
weakness is not the model but that a **new workspace is un-seeded and offers no entry point**
(see Deliverable 2), so the well-organized structure is invisible until the user imports.

## 6. Feature grouping — **grouped in docs, not in the UI**

The docs group features into 8 areas (APIs, Workflows, Testing, Stress, Analysis, Logs, Identity,
Settings). **The UI does not express these groups** — there is no rail, no section headers, no
grouping in the View menu. Related surfaces are scattered: Dashboard and Timeline (both "Analysis")
are two separate menu items with no visual kinship; Profiles/Environments/Variables are correctly
co-located in one tabbed panel (good), but the Environment switcher is on the toolbar while the
Profile concept has no switcher at all (inconsistent home for "Identity"). **S3.**

## 7. Property panels / inspectors — **inconsistent**

Only the Workflow Designer has a right-panel inspector (`NodePropertiesViewModel`). The Runner shows
request/response detail inline (no right inspector), and Testing has no inspector. The benchmark's
"one predictable place for detail editing" is therefore **not** a product-wide convention. Editing
also mixes paradigms: inline key-value rows in the Runner vs. modal dialogs for Explorer CRUD vs.
apply-immediately node config. **S3.**

## 8. Dialogs — **consistent and correct**

`Views/Dialogs/*` via `IDialogService`: create/edit for Service/Endpoint/Profile/Environment/
Variable/Assertion/TestCase, plus Import Wizard, Backup Settings, Name Prompt. Modal, single-purpose,
`IDialogService`-mediated. This layer is consistent and healthy. The only IA issue is **overuse**:
rename goes through a dialog where inline (F2) is expected (S3).

## 9. Context menus — **present in Explorer, absent as cross-feature launchers**

The Service Explorer has a rich context menu (add/edit/delete/duplicate/move). But context menus
are **not used to connect features**: there is no "Run", "Add Test Case", "Stress this", or
"Add to Workflow" verb on an endpoint or workflow. This is the single biggest *feature-IA* miss and
the main reason the product feels modular — the natural right-click gesture that would launch one
area from another doesn't exist. **S2.** (Detailed in Deliverable 7.)

## 10. Consistency — **strong on labels, weak on behavior**

- **Terminology:** the code honors the canonical label set (Workspace/Service/Endpoint/Profile/
  Environment/Variable/Workflow/Node/Run) — good lexical consistency.
- **Behavioral inconsistency is the problem:** Environment applies to requests but silently not to
  workflows; "Save" reports success but is a no-op; a Cancel button can't cancel; Replay is enabled
  where it's unsupported. The same *concept* behaves differently depending on where you are. This is
  more corrosive to the "one product" feel than any naming issue. **S2.**
- **Visual consistency** (tokens/styles) is assessed quantitatively in Deliverable 6.

---

## IA scorecard

| Aspect | Score /10 | Note |
|---|---|---|
| Navigation hierarchy | 4 | Primary tier (rail/palette/quick-open) entirely missing |
| Menu organization | 5 | Works but is the only nav; Test Cases + Settings homeless |
| Toolbar organization | 5 | Tidy but omits Import, Profile, Run, search |
| Dock layout | 8 | Correct tool/doc split; per-user (not per-workspace) scoping |
| Workspace organization | 7 | Sound model; empty/un-seeded on arrival |
| Feature grouping | 4 | 8 areas exist in docs, not expressed in UI |
| Property panels / inspector | 5 | Inspector only in Designer; mixed edit paradigms |
| Dialogs | 8 | Consistent, `IDialogService`-mediated; slightly overused |
| Context menus | 4 | Rich in Explorer; absent as cross-feature launchers |
| Consistency | 5 | Good labels; damaging behavioral inconsistencies |
| **Overall IA maturity** | **5.0/10** | Correct skeleton, missing primary nav + cross-feature links |

## The three IA moves that would most change the "one product" feel

1. **Add the Primary navigation tier** — Activity Rail (`Ctrl+1..5`) + Command Palette + Quick-Open.
   Turns a flat menu into a navigable product and makes breadth visible. (Fixes §1, §2, §6.)
2. **Make context menus the cross-feature fabric** — Run / Add Test Case / Stress / Add to Workflow
   on endpoints & workflows. Turns scattered modules into a connected tool. (Fixes §9 + Deliverable 7.)
3. **Enforce behavioral consistency** — one meaning per verb everywhere (Environment applies
   uniformly; Save persists or is removed; disabled = unsupported). Removes the "different tool per
   screen" feeling. (Fixes §10.)
