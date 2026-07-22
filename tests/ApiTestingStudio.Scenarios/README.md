# Executable Scenarios (ES) harness

An on-demand UI-automation harness that drives the **real** WPF app via
[FlaUI](https://github.com/FlaUI/FlaUI) (UI Automation) and emits a screenshot Markdown report.
Designed in Sprint 15 (Deliverable 13), built in Sprint 16 (item 8). It is the way the review's
findings become a **regression net**: each seed scenario doubles as an acceptance test for a
Phase-1 fix.

## Design

```
Model/        Scenario, ScenarioStep, Expectation  (data-only records — not a DSL)
Execution/    IScenarioProvider (the plugin seam), ScenarioRunner, ScenarioResult
Providers/    FlaUiScenarioProvider  (the one concrete provider for the MVP)
Reporting/    MarkdownReporter        (summary table + per-scenario steps + embedded screenshots)
Catalog/      SeedScenarios           (the highest-signal journeys)
```

- **`IScenarioProvider` is the seam.** The MVP ships `FlaUiScenarioProvider`; a future headless
  provider can implement the same contract without touching scenarios.
- Controls are located by their `AutomationProperties.Name`. Interaction goes through UIA patterns
  (Invoke/Value), so it can complete the native "New workspace" file dialog that raw `SendKeys` cannot.
- Not referenced by the app; it launches the built Host executable by path.

## Running

The seed run is **gated behind `ES_RUN=1`** so a normal `dotnet test` sweep does not spend minutes
driving the GUI (without it the test no-ops in milliseconds).

```bash
# Build first so the Host executable exists, then run on demand:
dotnet build ApiTestingStudio.slnx
ES_RUN=1 dotnet test tests/ApiTestingStudio.Scenarios --no-build
```

Override the app path with `ES_APP_EXE` if needed. Reports land in
`tests/ApiTestingStudio.Scenarios/reports/<timestamp>/` (git-ignored) as `report.md` plus PNGs.

> **Note:** the desktop session must be **unlocked** for meaningful screenshots. UIA assertions still
> pass while locked, but captures show the lock screen and virtualized tree items aren't realized.

## Seed scenarios

| # | Scenario | Guards |
|---|---|---|
| S-01 | First-run Welcome | PP-D1 (Welcome + CTAs), workspace-gating, PP-I1/D3 toolbar switchers |
| S-02 | Open sample workspace | PP-D6 (sample), PP-U6 (seeding), native-dialog automation, gated actions flip on |

Add more by appending records to `Catalog/SeedScenarios.cs`.
