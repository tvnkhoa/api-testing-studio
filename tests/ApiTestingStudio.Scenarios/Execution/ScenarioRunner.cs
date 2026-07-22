using System.Diagnostics;
using ApiTestingStudio.Scenarios.Model;

namespace ApiTestingStudio.Scenarios.Execution;

/// <summary>
/// Orchestrates a scenario against a provider: launch → capture "before" → run steps (screenshot each
/// state-changing one) → evaluate expectations → capture "after". A step or expectation failure is
/// recorded (not thrown) so the run always produces a full report. One provider instance per scenario.
/// </summary>
public sealed class ScenarioRunner
{
    private readonly Func<IScenarioProvider> _providerFactory;

    public ScenarioRunner(Func<IScenarioProvider> providerFactory) => _providerFactory = providerFactory;

    public async Task<ScenarioResult> RunAsync(Scenario scenario)
    {
        var sw = Stopwatch.StartNew();
        var steps = new List<StepResult>();
        var expectations = new List<ExpectationResult>();
        var screenshots = new List<string>();
        string? error = null;

        var provider = _providerFactory();
        try
        {
            await provider.LaunchAsync().ConfigureAwait(false);
            screenshots.Add(await provider.CaptureScreenshotAsync($"{Slug(scenario.Name)}-01-launch").ConfigureAwait(false));

            var i = 1;
            foreach (var step in scenario.Steps)
            {
                var result = await provider.InvokeAsync(step).ConfigureAwait(false);
                steps.Add(result);
                screenshots.Add(await provider.CaptureScreenshotAsync($"{Slug(scenario.Name)}-step-{++i:00}-{step.Action}").ConfigureAwait(false));
                if (!result.Success)
                {
                    break; // A failed step invalidates later ones; stop but still evaluate/report.
                }
            }

            foreach (var expectation in scenario.Expectations)
            {
                var observed = await provider.ObserveAsync(expectation).ConfigureAwait(false);
                expectations.Add(new ExpectationResult(expectation, observed.Success, observed.Detail));
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }
        finally
        {
            await provider.DisposeAsync().ConfigureAwait(false);
        }

        var passed = error is null
            && steps.All(s => s.Success)
            && expectations.All(e => e.Success)
            && expectations.Count > 0;

        return new ScenarioResult(
            scenario.Name, scenario.Goal, passed, steps, expectations, screenshots, sw.ElapsedMilliseconds, error);
    }

    private static string Slug(string name) =>
        string.Concat(name.Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-'));
}
