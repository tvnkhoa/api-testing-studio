# Deliverable 2 — First-Time User Experience Report

> Step 1 — Product Discovery. Exploring the product as a first-time user, before diving into
> implementation detail. What problem does it solve? How does a new user start? What is
> discoverable, what is hidden, what feels disconnected?

## The one-line pitch (what the product is)

A **workflow-first, 100%-offline API testing desktop app** for backend devs, QA, and DevOps.
Everything lives in a single portable **Workspace** (`.atsdb`). The intended differentiators over
Postman are **visual workflow automation, role/permission (profile) testing, stress testing, and
integrated test-case management** — all local, no cloud.

## First 60 seconds — what a new user actually experiences

Traced from `App.OnStartup` → `ShellWindow` → `ShellViewModel.InitializeAsync`:

1. The app launches with **no workspace open** — this is deliberate (`App.xaml.cs:143-146`).
2. The center shows a **Welcome document** whose body still reads *"Feature panels dock into this
   shell in the sprints ahead."* (`WelcomeDocumentViewModel.cs:21-23`) — **leftover Sprint-04
   placeholder copy shipped as the primary first impression.**
3. The status bar reads **"No workspace open"** (`StatusBarViewModel.cs:15`).
4. The only guidance offered is the sentence "Create or open a workspace from the File menu."
5. There is **no onboarding, no first-run detection, no product tour, no sample workspace.**
   (Grep for `onboarding|FirstRun|IsFirstLaunch` → nothing.)

**Verdict on first impression: S2 — High.** The single most important screen for activation is a
stale placeholder. A first-time user is told to go to a menu, with no sense of what the product
does or why it is different from Postman.

## The activation cliff (create → do something useful)

The intended journey (`UI_BENCHMARK.md` Journey 1 / `UI_INFORMATION_ARCHITECTURE.md` Flow 2) is:
`Ctrl+N` → name+location → **empty workspace opens with a Welcome doc showing 3 CTAs (Import APIs ·
Add Service · Open sample)** → *usable in 2 inputs.* Reality:

| Intended | Actual | Evidence |
|---|---|---|
| Name + location + description dialog | Bare `SaveFileDialog`; **name = filename, description always null** | `ShellViewModel.cs:221-222`, `FileDialogService.cs:41-53` |
| Seed variable scopes / default env / default profile | **Nothing seeded** — workspace is completely empty | `WorkspaceService.CreateAsync:26-62`, `SqliteStorageProvider.CreateAsync:39-76` |
| Welcome doc with 3 CTAs, reacts to state | Welcome VM has **zero commands**, view has **zero buttons**, never refreshes after create | `WelcomeDocumentViewModel.cs:8-24`, `WelcomeDocumentView.xaml` |
| "Open sample" | **No sample workspace exists** anywhere in the repo | (grep) |

So after creating a workspace, the user faces an **empty Service Explorer with nothing to click**
and a static Welcome doc that never changed. The only feedback is a transient status-bar line
"Created workspace '<name>'." **This is the single biggest activation gap in the product.**

## What is easy to discover

- **Workspace ops** — New / Open / Save are on the toolbar *and* File menu *and* have the expected
  `Ctrl+N/O/S` shortcuts. Recent-workspaces submenu works (`RecentWorkspacesMenuViewModel`).
- **Import** — reachable from the File menu; the wizard auto-detects format and previews before
  merge (genuinely good once you find it).
- **Theme toggle** — on the toolbar and in the View menu.
- **The Service Explorer tree** — once populated (e.g. via import), browsing/CRUD is strong and
  familiar (Rider-like).

## What feels hidden

- **Every functional area is buried one level deep in a single View menu.** There is **no Activity
  Rail, no Command Palette, no Quick-Open, no global search** (`(gap)` in the docs — confirmed
  absent in code; grep matched only the bundled Monaco JS). Nothing about the product's scope is
  visible at a glance.
- **Settings** — there is no Settings/Preferences screen. App preferences live *inside a dialog
  labelled "Backup Settings & Restore…"* (`ShellViewModel.cs:335-351`). A user looking for
  settings will not look under "Backup."
- **The Test Cases panel is a dead-end.** It is docked by default but has **no menu entry**
  (`MainMenuViewModel.cs:52-58`, absent from `ShellWindow.xaml`). Its toggle command exists but is
  unbound. **Close it once and there is no way to reopen it except Reset Layout.**
- **"Run As" profiles** — the toolbar has an Environment switcher but **no Profile switcher**, and
  no node/request field to choose a profile. The role-testing differentiator is invisible.

## What feels disconnected (first-impression level)

- **Workflows ignore your environment.** A new user sets an environment, builds a workflow with
  `{{baseUrl}}`, runs it — and it silently calls an empty URL, because the workflow run path never
  seeds variables (`WorkflowEditorViewModel.RunAsync:351-353`; `VariableScopeSeeder` only called by
  the ad-hoc Runner). No error, no warning.
- **Dashboard doesn't drill.** Clicking a chart point does nothing; there is no path from an
  interesting KPI to the run behind it. The analyze→investigate loop is broken at the click.
- **Testing/Stress can't be launched from what you're looking at.** There is no "Test this
  endpoint" / "Stress this endpoint" on the Explorer or Runner; you must open the panel and
  re-select the target from a global combo.

## Honest first-impression summary

A developer opening this for the first time would (a) not know what it does from the Welcome
screen, (b) create an empty workspace and hit a wall, (c) eventually find Import and get excited by
a populated tree and a competent Runner, then (d) start noticing that the advanced, differentiating
features (profiles, workflow variables, stress charts, dashboards) are either hidden or quietly
don't connect to each other. **The bones are strong and familiar; the first mile is missing.**

### Top 5 first-run fixes (preview — full detail in the Pain Point Backlog, Deliverable 11)

1. **Replace the placeholder Welcome with a real, state-aware first-run screen** (what it is + 3
   CTAs: Import APIs · Add Service · Open sample). — S2
2. **Ship a sample workspace** and wire "Open sample." — S2
3. **Make every area discoverable** (Activity Rail or, at minimum, fix the Test Cases dead-end and
   add all areas to the View menu with shortcuts). — S2
4. **Seed a new workspace** with default environment + variable scopes so imports/workflows have
   somewhere to resolve against. — S2
5. **Surface a Profile switcher** (or at least a Profile field on requests) so the role-testing
   pillar is visible and usable. — S2
