namespace ApiTestingStudio.UI.Services;

/// <summary>
/// Default in-memory <see cref="IStatusBarService"/>. Holds the latest background-status message and
/// notifies subscribers. Registered as a singleton so producers and the status-bar view model share
/// one instance.
/// </summary>
public sealed class StatusBarService : IStatusBarService
{
    public string Message { get; private set; } = string.Empty;

    public event EventHandler? MessageChanged;

    public void SetMessage(string message)
    {
        var normalized = message ?? string.Empty;
        if (string.Equals(normalized, Message, StringComparison.Ordinal))
        {
            return;
        }

        Message = normalized;
        MessageChanged?.Invoke(this, EventArgs.Empty);
    }
}
