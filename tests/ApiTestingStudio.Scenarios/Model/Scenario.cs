namespace ApiTestingStudio.Scenarios.Model;

/// <summary>The closed vocabulary of actions a scenario step can perform (kept deliberately small).</summary>
public enum StepAction
{
    /// <summary>Invoke a button/control located by its automation name.</summary>
    Click,

    /// <summary>Set the text of an editable control located by its automation name.</summary>
    TypeText,

    /// <summary>Wait for the given number of milliseconds (Value = ms).</summary>
    Wait,

    /// <summary>Complete the app's native "New/Save workspace" file dialog, saving to Value (a path).</summary>
    HandleSaveDialog,
}

/// <summary>The closed vocabulary of expectations a scenario can assert after its steps run.</summary>
public enum ExpectationKind
{
    /// <summary>A control with the given automation name exists somewhere in the window.</summary>
    ControlExists,

    /// <summary>A control with the given name exists and is enabled (Expected = "true"/"false").</summary>
    ControlEnabled,

    /// <summary>Some element's name contains the expected text (a loose "is this visible" check).</summary>
    TextVisible,

    /// <summary>The main window's title contains the expected text (readable via UIA even when the desktop is locked).</summary>
    WindowTitleContains,
}

/// <summary>One step in a scenario. <see cref="Target"/> is an automation name; <see cref="Value"/> is action-specific.</summary>
public sealed record ScenarioStep(StepAction Action, string Target, string? Value = null);

/// <summary>One post-condition to verify. <see cref="Target"/> is an automation name or search text.</summary>
public sealed record Expectation(ExpectationKind Kind, string Target, string? Expected = null);

/// <summary>
/// A data-only description of one end-to-end product journey: a name, a goal, ordered steps, and the
/// expectations that must hold afterwards. Scenarios are authored as C# records (not a DSL) and
/// executed by <c>ScenarioRunner</c> through an <c>IScenarioProvider</c>.
/// </summary>
public sealed record Scenario(
    string Name,
    string Goal,
    IReadOnlyList<ScenarioStep> Steps,
    IReadOnlyList<Expectation> Expectations);
