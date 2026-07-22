using ApiTestingStudio.Scenarios.Model;

namespace ApiTestingStudio.Scenarios.Execution;

/// <summary>
/// The plugin seam of the ES harness: something that can drive the app and read its state. The MVP
/// ships one implementation (<c>FlaUiScenarioProvider</c>); a future headless/in-process provider can
/// implement the same contract without touching scenarios or the runner.
/// </summary>
public interface IScenarioProvider : IAsyncDisposable
{
    /// <summary>Launch the app fresh (each scenario gets a clean instance).</summary>
    Task LaunchAsync();

    /// <summary>Perform one step; returns success + a human-readable detail for the report.</summary>
    Task<StepResult> InvokeAsync(ScenarioStep step);

    /// <summary>Evaluate one expectation against the current UI state.</summary>
    Task<ObservedState> ObserveAsync(Expectation expectation);

    /// <summary>Capture a PNG of the app window and return its file path (for the report).</summary>
    Task<string> CaptureScreenshotAsync(string label);
}
