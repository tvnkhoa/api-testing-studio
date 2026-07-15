using System.Text.Json;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Plugin.Abstractions.Importing;

namespace ApiTestingStudio.Import.Postman;

/// <summary>
/// Parses a Postman Collection (v2.x) JSON document into a single <see cref="Service"/> plus one
/// <see cref="Endpoint"/> per request. Folders are flattened; their names prefix the endpoint name.
/// </summary>
internal static class PostmanCollectionParser
{
    private static readonly JsonSerializerOptions HeaderJsonOptions = new(JsonSerializerDefaults.Web);

    public static ImportResult Parse(string content)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(content);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"The Postman collection is not valid JSON: {ex.Message}", ex);
        }

        using (document)
        {
            var root = document.RootElement;
            var serviceName = TryGetString(root, "info", "name") ?? "Imported Collection";

            var service = new Service { Name = serviceName };
            var endpoints = new List<Endpoint>();
            string? serviceBaseUrl = null;

            if (root.TryGetProperty("item", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                var sortOrder = 0;
                WalkItems(items, prefix: null, service.Id, endpoints, ref sortOrder, ref serviceBaseUrl);
            }

            service = service with { BaseUrl = serviceBaseUrl };
            return new ImportResult([service], endpoints);
        }
    }

    private static void WalkItems(
        JsonElement items,
        string? prefix,
        Guid serviceId,
        List<Endpoint> endpoints,
        ref int sortOrder,
        ref string? serviceBaseUrl)
    {
        foreach (var item in items.EnumerateArray())
        {
            var name = TryGetString(item, "name") ?? "Request";

            // A folder carries a nested "item" array; a request carries a "request" object.
            if (item.TryGetProperty("item", out var childItems) && childItems.ValueKind == JsonValueKind.Array)
            {
                var childPrefix = prefix is null ? name : $"{prefix} / {name}";
                WalkItems(childItems, childPrefix, serviceId, endpoints, ref sortOrder, ref serviceBaseUrl);
                continue;
            }

            if (!item.TryGetProperty("request", out var request))
            {
                continue;
            }

            var (baseUrl, path) = ExtractUrl(request);
            serviceBaseUrl ??= baseUrl;

            var method = ExtractMethod(request);
            var displayName = prefix is null ? name : $"{prefix} / {name}";

            endpoints.Add(new Endpoint
            {
                ServiceId = serviceId,
                Name = displayName,
                Method = method,
                Path = path,
                SortOrder = sortOrder++,
                DefaultHeaders = ExtractHeaders(request),
                DefaultBody = ExtractBody(request),
            });
        }
    }

    private static HttpVerb ExtractMethod(JsonElement request)
    {
        var method = TryGetString(request, "method");
        return !string.IsNullOrWhiteSpace(method) && Enum.TryParse<HttpVerb>(method, ignoreCase: true, out var verb)
            ? verb
            : HttpVerb.Get;
    }

    private static (string? BaseUrl, string Path) ExtractUrl(JsonElement request)
    {
        if (!request.TryGetProperty("url", out var url))
        {
            return (null, "/");
        }

        // url may be a bare string or an object with a "raw" member.
        var raw = url.ValueKind == JsonValueKind.String
            ? url.GetString()
            : TryGetString(url, "raw");

        if (string.IsNullOrWhiteSpace(raw))
        {
            return (null, "/");
        }

        if (Uri.TryCreate(raw, UriKind.Absolute, out var uri))
        {
            var path = uri.PathAndQuery.Length == 0 ? "/" : uri.PathAndQuery;
            return ($"{uri.Scheme}://{uri.Authority}", path);
        }

        // Templated ({{baseUrl}}/...) or relative URLs: keep the raw string as the path.
        return (null, raw);
    }

    private static string? ExtractHeaders(JsonElement request)
    {
        if (!request.TryGetProperty("header", out var headerArray) || headerArray.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var headers = new List<HttpHeader>();
        foreach (var header in headerArray.EnumerateArray())
        {
            var key = TryGetString(header, "key");
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var value = TryGetString(header, "value") ?? string.Empty;
            var disabled = header.TryGetProperty("disabled", out var d) && d.ValueKind == JsonValueKind.True;
            headers.Add(new HttpHeader(key, value, !disabled));
        }

        return headers.Count == 0 ? null : JsonSerializer.Serialize(headers, HeaderJsonOptions);
    }

    private static string? ExtractBody(JsonElement request)
    {
        if (!request.TryGetProperty("body", out var body) || body.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        // Only the "raw" body mode maps directly to a request body string.
        var raw = TryGetString(body, "raw");
        return string.IsNullOrWhiteSpace(raw) ? null : raw;
    }

    private static string? TryGetString(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(property, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string? TryGetString(JsonElement element, string property, string nested) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var child)
            ? TryGetString(child, nested)
            : null;
}
