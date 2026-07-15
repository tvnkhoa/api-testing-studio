using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace ApiTestingStudio.UI.Services;

/// <summary>
/// <see cref="IMonacoBridge"/> over a WPF <see cref="WebView2"/>. Maps a local folder to a virtual
/// https host so the editor loads its assets fully offline, and marshals text via
/// <c>window.editorApi</c> in <c>editor.html</c> (which self-degrades to a plain editor when the
/// Monaco <c>vs/</c> bundle has not been dropped into the asset folder).
/// </summary>
internal sealed class MonacoBridge : IMonacoBridge
{
    private const string VirtualHost = "apistudio.monaco";

    private readonly WebView2 _webView;

    public MonacoBridge(WebView2 webView) => _webView = webView;

    public bool IsReady { get; private set; }

    public event EventHandler<string>? TextChanged;

    public async Task InitializeAsync(string assetsFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetsFolder);

        await _webView.EnsureCoreWebView2Async().ConfigureAwait(true);
        var core = _webView.CoreWebView2;

        core.SetVirtualHostNameToFolderMapping(
            VirtualHost,
            assetsFolder,
            CoreWebView2HostResourceAccessKind.Allow);
        core.WebMessageReceived += OnWebMessageReceived;

        var loaded = new TaskCompletionSource();
        void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            core.NavigationCompleted -= OnNavigationCompleted;
            loaded.TrySetResult();
        }

        core.NavigationCompleted += OnNavigationCompleted;
        core.Navigate($"https://{VirtualHost}/editor.html");
        await loaded.Task.ConfigureAwait(true);

        IsReady = true;
    }

    public async Task PushAsync(string text, string language, bool isReadOnly)
    {
        if (!IsReady)
        {
            return;
        }

        var core = _webView.CoreWebView2;
        var script = $"window.editorApi && window.editorApi.apply("
            + $"{JsonSerializer.Serialize(text)},"
            + $"{JsonSerializer.Serialize(language)},"
            + $"{(isReadOnly ? "true" : "false")})";
        await core.ExecuteScriptAsync(script).ConfigureAwait(true);
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        => TextChanged?.Invoke(this, e.TryGetWebMessageAsString());
}
