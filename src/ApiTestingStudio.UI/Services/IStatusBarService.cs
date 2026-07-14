namespace ApiTestingStudio.UI.Services;

/// <summary>
/// A small sink for transient background-task / notification messages surfaced in the shell status
/// bar. Any service can push a message without referencing the status-bar view model; the view model
/// subscribes to <see cref="MessageChanged"/> and renders the latest text.
/// </summary>
public interface IStatusBarService
{
    /// <summary>The most recent background-status message (empty when idle).</summary>
    string Message { get; }

    /// <summary>Raised whenever <see cref="Message"/> changes.</summary>
    event EventHandler? MessageChanged;

    /// <summary>Sets the background-status message. Pass empty/whitespace to clear it.</summary>
    void SetMessage(string message);
}
