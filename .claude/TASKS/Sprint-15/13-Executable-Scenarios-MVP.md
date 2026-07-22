# Deliverable 13 — Executable Scenarios (ES) MVP Plan

> Step 10 — design (do **not** build) a minimal platform to *validate* future development by
> replaying the scenarios in Deliverable 3 against the real UI. The MVP must stay small: it is a
> validation harness, **not** a scripting language or test framework.

> **✅ BUILT (2026-07-22, Sprint 16):** implemented at `tests/ApiTestingStudio.Scenarios` using FlaUI
> (UIA3) per this design — data-only C# scenarios, `IScenarioProvider` seam, `ScenarioRunner`,
> `MarkdownReporter`, and seed scenarios S-01 (first-run Welcome) + S-02 (open sample workspace,
> which drives the native save dialog). Gated behind `ES_RUN=1`; emits a screenshot report under
> `reports/<timestamp>/`. First green run: **2/2 pass.** See the project README.

## Purpose & non-goals

**Purpose:** turn the 27 code-traced scenarios into *repeatable, screenshot-producing checks* that a
human (or CI) can run to confirm a journey still works after each consolidation change. This is how
Sprint 15's findings become a regression net for Sprint 16+.

**Non-goals (hard boundaries):**
- ❌ No DSL / scripting language — scenarios are plain C# records or a thin YAML/JSON, not code users write.
- ❌ No general test framework — reuse xUnit as the runner; ES only adds UI-driving + reporting.
- ❌ No coverage of unit-testable logic — ES is for *end-to-end product journeys* only.
- ❌ Not shipped in the product — it lives under `tests/`, runs on demand/CI, never in the app.

## Architecture (fits the existing Clean + Plugin model)

```
tests/ApiTestingStudio.Scenarios/           (new test project — NOT referenced by the app)
├── Model/
│   ├── Scenario.cs            record: Name, Goal, Prerequisites, Steps[], Expectations[]
│   ├── ScenarioStep.cs        record: Action (enum) + Target + Value
│   └── Expectation.cs         record: Kind (TextVisible/ControlEnabled/StatusEquals/…) + Target + Expected
├── Execution/
│   ├── IScenarioProvider.cs   abstraction — drives the app, captures state + screenshots
│   ├── ScenarioRunner.cs      orchestrates: prerequisites → steps → expectations → report
│   └── ScenarioResult.cs      record: per-step + per-expectation pass/fail, timings, screenshot paths
├── Providers/
│   └── FlaUiScenarioProvider.cs   the one concrete provider for the MVP
├── Reporting/
│   └── MarkdownReporter.cs    emits a per-run Markdown report + embedded screenshots
└── Catalog/
    └── *.scenario.json        the scenarios from Deliverable 3, data-only
```

**Key design choice — `IScenarioProvider` is the plugin seam.** The MVP ships **one**
implementation (`FlaUiScenarioProvider`, using **FlaUI** for Windows UI automation). Because the app
already has ~80 `AutomationProperties.Name` (Deliverable 6 §2), FlaUI can locate most controls today
— the accessibility investment pays off directly here. A future provider (e.g. a headless in-process
harness) can implement the same interface without touching scenarios.

## The provider contract

```csharp
public interface IScenarioProvider : IAsyncDisposable
{
    Task LaunchAsync(string? workspacePath = null);          // start the app (optionally open a workspace)
    Task<bool> InvokeAsync(ScenarioStep step);               // Click/Type/Select/DoubleClick/Menu/Key
    Task<ObservedState> ObserveAsync(Expectation e);         // read control text/enabled/visible/status
    Task<string> CaptureScreenshotAsync(string label);       // PNG path for the report
}
```

`ScenarioStep.Action` (small closed enum): `OpenMenu, Click, DoubleClick, TypeText, SelectItem,
PressKey, WaitFor`. `Expectation.Kind`: `TextVisible, ControlEnabled, ControlExists, StatusBarEquals,
TreeContains, TabOpen`. That vocabulary covers every scenario in Deliverable 3 without becoming a language.

## Scenario format (data, not code)

```json
{
  "name": "S-04 Import OpenAPI",
  "goal": "Populate the catalog from an OpenAPI file",
  "prerequisites": ["freshWorkspace"],
  "steps": [
    { "action": "OpenMenu", "target": "File>Import Package" },
    { "action": "TypeText", "target": "SourcePath", "value": "{fixtures}/petstore.json" },
    { "action": "Click", "target": "PreviewButton" },
    { "action": "Click", "target": "MergeButton" }
  ],
  "expectations": [
    { "kind": "StatusBarEquals", "expected": "Import complete." },
    { "kind": "TreeContains", "target": "ServiceExplorer", "expected": "Swagger Petstore" }
  ]
}
```

## MVP scope — the 8 seed scenarios (highest-signal subset)

Not all 27 at once. Seed with the journeys that (a) are core and (b) exercise the Phase-1 fixes, so
the harness immediately guards the riskiest work:

1. S-02 Create Workspace (seeded) · 2. S-04 Import OpenAPI · 3. S-09 Execute Request ·
4. S-10 Inspect Response · 5. S-13/S-16 Environment→Workflow resolves (the PP-W1 guard) ·
6. S-14 Switch Profile applies auth (the PP-I1 guard) · 7. S-19 Timeline replay ·
8. S-22/S-23 Export→Reopen round-trip.

Each seed scenario doubles as an **acceptance test for a Phase-1 roadmap item** — the harness is born
guarding exactly what Sprint 16 will change.

## Reporting

`MarkdownReporter` emits, per run: a summary table (scenario → pass/fail → duration), and per
scenario the steps, expectation results, and **embedded before/after screenshots**. Output lands in
`tests/ApiTestingStudio.Scenarios/reports/<timestamp>/`. This is the artifact Sprint 15 could not
produce (real screenshots) and the reason the ES MVP exists.

## Effort & sequencing

| Piece | Scope |
|---|---|
| Model + runner + Markdown reporter | S (2–3 days) |
| `FlaUiScenarioProvider` (launch, invoke, observe, screenshot) | M (4–6 days) |
| 8 seed scenarios + fixtures (sample spec, sample workspace) | S (2 days) |
| CI wiring (on-demand job; not per-commit — UI runs are slow) | S (1 day) |

**Total ≈ M (≈2 weeks).** Recommended to build **alongside Phase 1** of the roadmap so each Phase-1
fix ships with its ES guard. **Prerequisite to add:** a couple of missing `AutomationProperties.Name`
(PP-A3) and stable `AutomationId`s on the controls the seed scenarios touch.

## Why this is the right MVP

- It is **small and bounded** — one provider, a closed action vocabulary, data-only scenarios.
- It **reuses existing investments** — accessibility names, the DI/plugin mindset, xUnit.
- It **directly de-risks consolidation** — the seed set guards the exact journeys Sprint 16 repairs.
- It is **extensible without scope creep** — new providers/scenarios drop in behind `IScenarioProvider`.
