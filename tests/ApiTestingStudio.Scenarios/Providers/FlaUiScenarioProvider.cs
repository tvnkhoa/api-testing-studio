using System.IO;
using ApiTestingStudio.Scenarios.Execution;
using ApiTestingStudio.Scenarios.Model;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;

namespace ApiTestingStudio.Scenarios.Providers;

/// <summary>
/// Drives the real WPF app through UI Automation (FlaUI/UIA3). Because it launches the app itself,
/// control interaction goes through UIA patterns (Invoke/Value) in the same session — no synthetic
/// keyboard/mouse input — which is why it can complete the native "New workspace" file dialog that a
/// raw SendKeys cannot. Controls are located by their <c>AutomationProperties.Name</c>.
/// </summary>
public sealed class FlaUiScenarioProvider : IScenarioProvider
{
    private static readonly TimeSpan FindTimeout = TimeSpan.FromSeconds(8);

    private readonly string _exePath;
    private readonly string _screenshotDir;

    private Application? _app;
    private UIA3Automation? _automation;
    private Window? _window;

    public FlaUiScenarioProvider(string exePath, string screenshotDir)
    {
        _exePath = exePath;
        _screenshotDir = screenshotDir;
        Directory.CreateDirectory(_screenshotDir);
    }

    public Task LaunchAsync()
    {
        _automation = new UIA3Automation();
        _app = Application.Launch(_exePath);
        _window = _app.GetMainWindow(_automation, FindTimeout)
            ?? throw new InvalidOperationException("The app window did not appear.");
        _window.Focus();
        return Task.CompletedTask;
    }

    public Task<StepResult> InvokeAsync(ScenarioStep step)
    {
        try
        {
            switch (step.Action)
            {
                case StepAction.Click:
                    var button = FindByName(step.Target)
                        ?? throw new InvalidOperationException($"Control '{step.Target}' not found.");
                    button.AsButton().Invoke();
                    break;

                case StepAction.TypeText:
                    var box = FindByName(step.Target)
                        ?? throw new InvalidOperationException($"Control '{step.Target}' not found.");
                    box.AsTextBox().Text = step.Value ?? string.Empty;
                    break;

                case StepAction.Wait:
                    Thread.Sleep(int.TryParse(step.Value, out var ms) ? ms : 500);
                    break;

                case StepAction.HandleSaveDialog:
                    CompleteSaveDialog(step.Value ?? throw new InvalidOperationException("HandleSaveDialog needs a path."));
                    break;
            }

            return Task.FromResult(new StepResult(step, true, $"{step.Action} '{step.Target}' ok"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new StepResult(step, false, $"{step.Action} '{step.Target}' failed: {ex.Message}"));
        }
    }

    public Task<ObservedState> ObserveAsync(Expectation expectation)
    {
        try
        {
            switch (expectation.Kind)
            {
                case ExpectationKind.ControlExists:
                {
                    var el = FindByName(expectation.Target);
                    return Task.FromResult(new ObservedState(el is not null, el is not null ? "exists" : "not found"));
                }

                case ExpectationKind.ControlEnabled:
                {
                    var el = FindByName(expectation.Target);
                    var wantEnabled = !string.Equals(expectation.Expected, "false", StringComparison.OrdinalIgnoreCase);
                    if (el is null)
                    {
                        return Task.FromResult(new ObservedState(false, "control not found"));
                    }

                    var enabled = el.IsEnabled;
                    return Task.FromResult(new ObservedState(enabled == wantEnabled, $"enabled={enabled}, wanted={wantEnabled}"));
                }

                case ExpectationKind.TextVisible:
                {
                    var el = _window!.FindFirstDescendant(cf => cf.ByName(expectation.Target));
                    return Task.FromResult(new ObservedState(el is not null, el is not null ? "visible" : "not visible"));
                }

                case ExpectationKind.WindowTitleContains:
                {
                    var title = _window!.Title ?? string.Empty;
                    var ok = title.Contains(expectation.Target, StringComparison.OrdinalIgnoreCase);
                    return Task.FromResult(new ObservedState(ok, $"title='{title}'"));
                }

                default:
                    return Task.FromResult(new ObservedState(false, "unknown expectation"));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ObservedState(false, ex.Message));
        }
    }

    public Task<string> CaptureScreenshotAsync(string label)
    {
        var path = Path.Combine(_screenshotDir, $"{label}.png");
        try
        {
            _window?.Focus();
            Thread.Sleep(300);
            Capture.Element(_window!).ToFile(path);
        }
        catch
        {
            // Fall back to a full-screen capture if element capture fails.
            Capture.Screen().ToFile(path);
        }

        return Task.FromResult(path);
    }

    /// <summary>Finds a descendant by automation name, retrying while the UI settles.</summary>
    private AutomationElement? FindByName(string name) =>
        Retry.WhileNull(
            () => _window!.FindFirstDescendant(cf => cf.ByName(name)),
            FindTimeout,
            TimeSpan.FromMilliseconds(250)).Result;

    /// <summary>Completes the modal "New/Save workspace" file dialog: set the file name, click Save.</summary>
    private void CompleteSaveDialog(string savePath)
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }

        var dialog = Retry.WhileNull(
            () => _window!.ModalWindows.FirstOrDefault()
                  ?? _automation!.GetDesktop().FindFirstChild(cf => cf.ByClassName("#32770")),
            FindTimeout,
            TimeSpan.FromMilliseconds(250)).Result
            ?? throw new InvalidOperationException("The save dialog did not appear.");

        // The file-name field: prefer the edit inside the "File name:" combo; fall back to a writable edit.
        var fileNameEdit =
            dialog.FindFirstDescendant(cf => cf.ByControlType(ControlType.Edit).And(cf.ByName("File name:")))
            ?? dialog.FindFirstDescendant(cf => cf.ByControlType(ControlType.ComboBox).And(cf.ByName("File name:")))
                ?.FindFirstDescendant(cf => cf.ByControlType(ControlType.Edit))
            ?? dialog.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit))
                .FirstOrDefault(e => e.Patterns.Value.IsSupported && !e.Patterns.Value.Pattern.IsReadOnly);

        if (fileNameEdit is null)
        {
            throw new InvalidOperationException("The file-name field was not found in the save dialog.");
        }

        fileNameEdit.Focus();
        fileNameEdit.AsTextBox().Text = savePath;

        var save = dialog.FindFirstDescendant(cf => cf.ByName("Save").And(cf.ByControlType(ControlType.Button)))
            ?? dialog.FindFirstDescendant(cf => cf.ByName("Save"));
        if (save is null)
        {
            throw new InvalidOperationException("The Save button was not found in the save dialog.");
        }

        save.AsButton().Invoke();

        // Wait for the workspace to actually be created on disk (the app seeds + opens it).
        Retry.WhileFalse(() => File.Exists(savePath), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(300));
        Thread.Sleep(1500); // let the shell finish opening + refreshing panels
    }

    public ValueTask DisposeAsync()
    {
        try { _app?.Close(); } catch { /* ignore */ }
        try { if (_app is not null && !_app.HasExited) { _app.Kill(); } } catch { /* ignore */ }
        _automation?.Dispose();
        return ValueTask.CompletedTask;
    }
}
