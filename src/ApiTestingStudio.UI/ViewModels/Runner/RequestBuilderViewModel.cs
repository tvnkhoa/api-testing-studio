using System.Collections.ObjectModel;
using System.Text.Json;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTestingStudio.UI.ViewModels.Runner;

/// <summary>
/// The request-building surface: method, URL, query params, headers and body. Assembles an
/// immutable <see cref="HttpRequestModel"/> for execution and can be populated from an endpoint's
/// defaults or from a replayed request.
/// </summary>
public sealed partial class RequestBuilderViewModel : ObservableObject
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [ObservableProperty]
    private HttpVerb _method = HttpVerb.Get;

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private BodyKind _bodyKind = BodyKind.Json;

    /// <summary>Available HTTP verbs for the method selector.</summary>
    public IReadOnlyList<HttpVerb> Methods { get; } = Enum.GetValues<HttpVerb>();

    /// <summary>Available body kinds for the body-type selector.</summary>
    public IReadOnlyList<BodyKind> BodyKinds { get; } = Enum.GetValues<BodyKind>();

    public ObservableCollection<KeyValueRowViewModel> Headers { get; } = [];

    public ObservableCollection<KeyValueRowViewModel> QueryParams { get; } = [];

    /// <summary>Editor for the request body.</summary>
    public MonacoEditorViewModel Body { get; } = new(language: "json");

    /// <summary>Builds the immutable request model from the current builder state.</summary>
    public HttpRequestModel Build() => new()
    {
        Method = Method,
        Url = Url.Trim(),
        QueryParams = QueryParams
            .Select(r => new QueryParam(r.Name, r.Value, r.Enabled))
            .ToList(),
        Headers = Headers
            .Select(r => new HttpHeader(r.Name, r.Value, r.Enabled))
            .ToList(),
        BodyKind = BodyKind,
        Body = string.IsNullOrEmpty(Body.Text) ? null : Body.Text,
    };

    /// <summary>Populates the builder from an endpoint and its service base URL.</summary>
    public void LoadFromEndpoint(Endpoint endpoint, string? baseUrl)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        Method = endpoint.Method;
        Url = CombineUrl(baseUrl, endpoint.Path);
        BodyKind = BodyKind.Json;

        Headers.Clear();
        foreach (var header in DeserializeHeaders(endpoint.DefaultHeaders))
        {
            Headers.Add(new KeyValueRowViewModel { Name = header.Name, Value = header.Value, Enabled = header.Enabled });
        }

        QueryParams.Clear();
        Body.Text = endpoint.DefaultBody ?? string.Empty;
    }

    /// <summary>Populates the builder from a stored request (history replay).</summary>
    public void LoadFromRequest(HttpRequestModel request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Method = request.Method;
        Url = request.Url;
        BodyKind = request.BodyKind;

        Headers.Clear();
        foreach (var header in request.Headers)
        {
            Headers.Add(new KeyValueRowViewModel { Name = header.Name, Value = header.Value, Enabled = header.Enabled });
        }

        QueryParams.Clear();
        foreach (var param in request.QueryParams)
        {
            QueryParams.Add(new KeyValueRowViewModel { Name = param.Name, Value = param.Value, Enabled = param.Enabled });
        }

        Body.Text = request.Body ?? string.Empty;
    }

    private static List<HttpHeader> DeserializeHeaders(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<HttpHeader>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string CombineUrl(string? baseUrl, string path)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return path;
        }

        if (string.IsNullOrEmpty(path))
        {
            return baseUrl;
        }

        return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }
}
