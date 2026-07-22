using ApiTestingStudio.Scenarios.Model;

namespace ApiTestingStudio.Scenarios.Catalog;

/// <summary>
/// The seed scenario set — the highest-signal journeys that also exercise the Sprint-16 Phase-1
/// fixes, so the harness guards exactly what changed. Each is data-only; the runner drives them.
/// </summary>
public static class SeedScenarios
{
    public static IReadOnlyList<Scenario> All(string sampleWorkspacePath) =>
    [
        // S-01 — First-run onboarding surface (Welcome + CTAs + workspace gating). Guards PP-D1/PP-U6.
        new Scenario(
            "S-01 First-run Welcome",
            "A new user is greeted by a product-explaining Welcome with working CTAs, gated correctly.",
            Steps: [],
            Expectations:
            [
                new Expectation(ExpectationKind.TextVisible, "API Testing Studio"),
                new Expectation(ExpectationKind.ControlExists, "Open sample workspace"),
                new Expectation(ExpectationKind.ControlEnabled, "Open sample workspace", "true"),
                new Expectation(ExpectationKind.ControlEnabled, "Import", "false"),
                new Expectation(ExpectationKind.ControlEnabled, "Add service", "false"),
                // The toolbar "Run As" + environment switchers exist even before a workspace opens.
                new Expectation(ExpectationKind.ControlExists, "Active Run As profile"),
                new Expectation(ExpectationKind.ControlExists, "Active environment"),
            ]),

        // S-02 — Open the sample workspace (drives the native save dialog → populated catalog).
        // Guards PP-D6 (sample), PP-U6 (seeding), and confirms the workspace-gated actions flip on.
        new Scenario(
            "S-02 Open sample workspace",
            "The Open-sample CTA builds a seeded sample workspace and opens it, populating the catalog.",
            Steps:
            [
                new ScenarioStep(StepAction.Click, "Open sample workspace"),
                new ScenarioStep(StepAction.HandleSaveDialog, "SaveDialog", sampleWorkspacePath),
                new ScenarioStep(StepAction.Wait, "settle", "1500"),
            ],
            Expectations:
            [
                // The opened sample workspace is reflected in the window title (readable even if the
                // desktop is locked; the Service Explorer tree virtualizes and needs rendering).
                new Expectation(ExpectationKind.WindowTitleContains, "Sample API Workspace"),
                // Workspace-gated actions are now enabled.
                new Expectation(ExpectationKind.ControlEnabled, "Import", "true"),
                new Expectation(ExpectationKind.ControlEnabled, "Add service", "true"),
                // The "Run As" and environment switchers remain present with a workspace open.
                new Expectation(ExpectationKind.ControlExists, "Active Run As profile"),
                new Expectation(ExpectationKind.ControlExists, "Active environment"),
            ]),
    ];
}
