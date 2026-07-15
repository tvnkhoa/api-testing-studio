namespace ApiTestingStudio.UI.Services;

/// <summary>
/// Wraps the JS interop between WPF and the Monaco editor hosted in a WebView2. Isolates the view
/// code-behind from the CoreWebView2 details and lets the offline editor asset host be swapped.
/// </summary>
public interface IMonacoBridge
{
    /// <summary>Whether the hosted editor has finished loading and is ready for interop.</summary>
    bool IsReady { get; }

    /// <summary>Raised when the user edits the hosted editor; carries the new text.</summary>
    event EventHandler<string>? TextChanged;

    /// <summary>Initializes the WebView2, maps the offline asset folder and loads the editor.</summary>
    Task InitializeAsync(string assetsFolder);

    /// <summary>Pushes text, language and read-only state into the hosted editor.</summary>
    Task PushAsync(string text, string language, bool isReadOnly);
}
