using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ApiTestingStudio.UI.ViewModels.Runner;

/// <summary>
/// View model backing a Monaco-hosted code editor (request body / response body). The view syncs
/// <see cref="Text"/> to and from the WebView2-hosted editor via <see cref="Services.IMonacoBridge"/>;
/// formatting is done here in C# so it works regardless of whether the Monaco assets are present.
/// </summary>
public sealed partial class MonacoEditorViewModel : ObservableObject
{
    private static readonly JsonSerializerOptions IndentedOptions = new() { WriteIndented = true };

    public MonacoEditorViewModel(string language = "json", bool isReadOnly = false)
    {
        _language = language;
        _isReadOnly = isReadOnly;
    }

    /// <summary>The editor contents.</summary>
    [ObservableProperty]
    private string _text = string.Empty;

    /// <summary>Monaco language id (e.g. <c>json</c>, <c>plaintext</c>).</summary>
    [ObservableProperty]
    private string _language;

    /// <summary>Whether the editor is read-only (used for the response viewer).</summary>
    [ObservableProperty]
    private bool _isReadOnly;

    /// <summary>Pretty-prints the current text as JSON; leaves it unchanged if it is not valid JSON.</summary>
    [RelayCommand]
    private void Format()
    {
        if (string.IsNullOrWhiteSpace(Text))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(Text);
            Text = JsonSerializer.Serialize(document, IndentedOptions);
        }
        catch (JsonException)
        {
            // Not JSON — leave the text as-is.
        }
    }
}
