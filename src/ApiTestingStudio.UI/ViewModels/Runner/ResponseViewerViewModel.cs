using System.Collections.ObjectModel;
using ApiTestingStudio.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTestingStudio.UI.ViewModels.Runner;

/// <summary>
/// Displays the most recent response: status, headers, body (Monaco, read-only), size and timing.
/// </summary>
public sealed partial class ResponseViewerViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _hasResponse;

    [ObservableProperty]
    private int _statusCode;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private bool _isSuccessStatus;

    [ObservableProperty]
    private string _sizeText = string.Empty;

    [ObservableProperty]
    private string _timingText = string.Empty;

    public ObservableCollection<HttpHeader> Headers { get; } = [];

    /// <summary>Read-only editor showing the response body.</summary>
    public MonacoEditorViewModel Body { get; } = new(language: "json", isReadOnly: true);

    /// <summary>Populates the viewer from an execution result.</summary>
    public void Show(HttpExecutionResult execution)
    {
        ArgumentNullException.ThrowIfNull(execution);

        var response = execution.Response;
        StatusCode = response.StatusCode;
        StatusText = $"{response.StatusCode} {response.ReasonPhrase}".Trim();
        IsSuccessStatus = response.StatusCode is >= 200 and < 300;
        SizeText = FormatSize(response.ContentLengthBytes);
        TimingText = FormatTiming(execution.Timing);

        Headers.Clear();
        foreach (var header in response.Headers)
        {
            Headers.Add(header);
        }

        Body.Text = response.Body ?? string.Empty;
        HasResponse = true;
    }

    private static string FormatSize(long bytes) =>
        bytes < 1024 ? $"{bytes} B" : $"{bytes / 1024.0:0.#} KB";

    private static string FormatTiming(RequestTiming timing)
    {
        var parts = new List<string> { $"total {timing.Total.TotalMilliseconds:0} ms" };
        if (timing.Dns is { } dns)
        {
            parts.Add($"dns {dns.TotalMilliseconds:0} ms");
        }

        if (timing.Connect is { } connect)
        {
            parts.Add($"connect {connect.TotalMilliseconds:0} ms");
        }

        if (timing.TimeToFirstByte is { } ttfb)
        {
            parts.Add($"ttfb {ttfb.TotalMilliseconds:0} ms");
        }

        return string.Join(" · ", parts);
    }
}
