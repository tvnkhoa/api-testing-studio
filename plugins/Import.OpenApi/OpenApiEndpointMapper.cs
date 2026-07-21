using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Plugin.Abstractions.Importing;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using Microsoft.OpenApi.YamlReader;

namespace ApiTestingStudio.Import.OpenApi;

/// <summary>
/// Maps an OpenAPI 3.1 / 3.0 / Swagger 2.0 document (JSON or YAML) into a single <see cref="Service"/>
/// plus one <see cref="Endpoint"/> per (path, HTTP method) operation. Shared by the OpenAPI and Scalar
/// importers (Scalar reference endpoints expose an underlying OpenAPI document).
/// </summary>
public static class OpenApiEndpointMapper
{
    private static readonly JsonSerializerOptions HeaderJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions BodyJsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>Recursion guard for the body-skeleton generator (defends against $ref cycles).</summary>
    private const int MaxSchemaDepth = 8;

    /// <summary>Parses raw OpenAPI text (JSON or YAML) and maps it. Throws on unparseable input.</summary>
    public static ImportResult Map(string content, string? fallbackName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        // v2 registers the JSON reader by default; add YAML so we keep accepting both formats. The
        // format hint is derived from the content (a JSON document starts with '{').
        var settings = new OpenApiReaderSettings();
        settings.AddYamlReader();

        var format = content.TrimStart().StartsWith('{') ? OpenApiConstants.Json : OpenApiConstants.Yaml;
        var result = OpenApiDocument.Parse(content, format, settings);

        var document = result.Document;
        if (document is null)
        {
            var reasons = result.Diagnostic?.Errors.Count > 0
                ? string.Join("; ", result.Diagnostic.Errors.Select(e => e.Message))
                : "unknown error";
            throw new InvalidOperationException($"The OpenAPI document could not be parsed: {reasons}");
        }

        // A document with no paths and reader errors is effectively unusable — surface the reasons.
        if ((document.Paths is null || document.Paths.Count == 0) && result.Diagnostic?.Errors.Count > 0)
        {
            var reasons = string.Join("; ", result.Diagnostic.Errors.Select(e => e.Message));
            throw new InvalidOperationException($"The OpenAPI document is invalid: {reasons}");
        }

        return Map(document, fallbackName);
    }

    /// <summary>Maps an already-parsed OpenAPI document.</summary>
    public static ImportResult Map(OpenApiDocument document, string? fallbackName = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        var serviceName = document.Info?.Title is { Length: > 0 } title
            ? title
            : (string.IsNullOrWhiteSpace(fallbackName) ? "Imported API" : fallbackName!);

        var baseUrl = document.Servers?.FirstOrDefault()?.Url;

        var service = new Service
        {
            Name = serviceName,
            BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl,
            Description = string.IsNullOrWhiteSpace(document.Info?.Description) ? null : document.Info!.Description,
        };

        var endpoints = new List<Endpoint>();
        var sortOrder = 0;

        if (document.Paths is not null)
        {
            foreach (var (path, item) in document.Paths)
            {
                if (item?.Operations is null)
                {
                    continue;
                }

                foreach (var (method, operation) in item.Operations)
                {
                    if (!TryMapVerb(method, out var verb))
                    {
                        continue;
                    }

                    endpoints.Add(new Endpoint
                    {
                        ServiceId = service.Id,
                        Name = BuildName(operation, verb, path),
                        Method = verb,
                        // Path templates (e.g. /orders/{id}) are preserved as-is; query parameters are
                        // appended as an editable template (?limit=&offset=) since the endpoint has no
                        // dedicated query storage — the runner surfaces them in the URL.
                        Path = BuildPath(path, operation),
                        Description = FirstNonEmpty(operation.Summary, operation.Description),
                        SortOrder = sortOrder++,
                        DefaultHeaders = BuildHeaders(operation),
                        DefaultBody = BuildBody(operation),
                    });
                }
            }
        }

        return new ImportResult([service], endpoints);
    }

    private static string BuildName(OpenApiOperation operation, HttpVerb verb, string path) =>
        FirstNonEmpty(operation.OperationId, operation.Summary)
            ?? $"{verb.ToString().ToUpperInvariant()} {path}";

    private static string? BuildHeaders(OpenApiOperation operation)
    {
        if (operation.Parameters is null)
        {
            return null;
        }

        var headers = operation.Parameters
            .Where(p => p.In == ParameterLocation.Header && !string.IsNullOrWhiteSpace(p.Name))
            .Select(p => new HttpHeader(p.Name!, string.Empty))
            .ToList();

        return headers.Count == 0 ? null : JsonSerializer.Serialize(headers, HeaderJsonOptions);
    }

    /// <summary>
    /// Appends query parameters as an editable template (<c>?name=&amp;name2=</c>) to the path so the
    /// runner surfaces them. Path parameters already live in the path template and are left untouched.
    /// </summary>
    private static string BuildPath(string path, OpenApiOperation operation)
    {
        if (operation.Parameters is null)
        {
            return path;
        }

        var query = operation.Parameters
            .Where(p => p.In == ParameterLocation.Query && !string.IsNullOrWhiteSpace(p.Name))
            .Select(p => $"{Uri.EscapeDataString(p.Name!)}=")
            .ToList();

        return query.Count == 0 ? path : $"{path}?{string.Join('&', query)}";
    }

    /// <summary>
    /// Builds a pre-filled JSON request body from the operation's <c>application/json</c> request body:
    /// the media type's example when present, otherwise a skeleton generated from its schema. Returns
    /// null when the operation has no JSON body.
    /// </summary>
    private static string? BuildBody(OpenApiOperation operation)
    {
        var content = operation.RequestBody?.Content;
        if (content is null || content.Count == 0)
        {
            return null;
        }

        var media = SelectJsonMedia(content);
        if (media is null)
        {
            return null;
        }

        var sample = media.Example is not null
            ? media.Example.DeepClone()
            : (media.Schema is not null ? SampleFromSchema(media.Schema, 0) : null);

        return sample?.ToJsonString(BodyJsonOptions);
    }

    /// <summary>Picks the JSON media type from a request body's content, or null if none is JSON.</summary>
    private static OpenApiMediaType? SelectJsonMedia(IDictionary<string, OpenApiMediaType> content)
    {
        foreach (var (mediaType, value) in content)
        {
            if (mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        foreach (var (mediaType, value) in content)
        {
            if (mediaType.Contains("json", StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// Produces a representative <see cref="JsonNode"/> for a schema: explicit example/default/enum
    /// when given, otherwise a value shaped by the schema's type (object → properties, array → one
    /// item, primitives → empty/zero/false). Bounded by <see cref="MaxSchemaDepth"/> to survive cycles.
    /// </summary>
    private static JsonNode? SampleFromSchema(IOpenApiSchema schema, int depth)
    {
        if (depth > MaxSchemaDepth)
        {
            return null;
        }

        if (schema.Example is not null) { return schema.Example.DeepClone(); }
        if (schema.Default is not null) { return schema.Default.DeepClone(); }
        if (schema.Enum is { Count: > 0 } enumValues && enumValues[0] is not null) { return enumValues[0]!.DeepClone(); }

        if (schema.AllOf is { Count: > 0 } allOf)
        {
            var merged = new JsonObject();
            foreach (var part in allOf)
            {
                if (SampleFromSchema(part, depth + 1) is JsonObject partObject)
                {
                    foreach (var (key, node) in partObject)
                    {
                        merged[key] = node?.DeepClone();
                    }
                }
            }

            return merged;
        }

        if (schema.OneOf is { Count: > 0 } oneOf) { return SampleFromSchema(oneOf[0], depth + 1); }
        if (schema.AnyOf is { Count: > 0 } anyOf) { return SampleFromSchema(anyOf[0], depth + 1); }

        var type = schema.Type;
        var isObject = (type.HasValue && type.Value.HasFlag(JsonSchemaType.Object)) || schema.Properties is { Count: > 0 };
        if (isObject)
        {
            var obj = new JsonObject();
            if (schema.Properties is not null)
            {
                foreach (var (name, property) in schema.Properties)
                {
                    obj[name] = SampleFromSchema(property, depth + 1);
                }
            }

            return obj;
        }

        var isArray = (type.HasValue && type.Value.HasFlag(JsonSchemaType.Array)) || schema.Items is not null;
        if (isArray)
        {
            var array = new JsonArray();
            if (schema.Items is not null && SampleFromSchema(schema.Items, depth + 1) is { } item)
            {
                array.Add(item);
            }

            return array;
        }

        if (type.HasValue)
        {
            if (type.Value.HasFlag(JsonSchemaType.String)) { return JsonValue.Create(string.Empty); }
            if (type.Value.HasFlag(JsonSchemaType.Integer)) { return JsonValue.Create(0); }
            if (type.Value.HasFlag(JsonSchemaType.Number)) { return JsonValue.Create(0d); }
            if (type.Value.HasFlag(JsonSchemaType.Boolean)) { return JsonValue.Create(false); }
        }

        return null;
    }

    /// <summary>
    /// Maps the HTTP method key (v2 keys operations by <see cref="HttpMethod"/>) to our
    /// <see cref="HttpVerb"/>. TRACE has no <see cref="HttpVerb"/> equivalent and is skipped.
    /// </summary>
    private static bool TryMapVerb(HttpMethod method, out HttpVerb verb)
    {
        if (method == HttpMethod.Get) { verb = HttpVerb.Get; return true; }
        if (method == HttpMethod.Post) { verb = HttpVerb.Post; return true; }
        if (method == HttpMethod.Put) { verb = HttpVerb.Put; return true; }
        if (method == HttpMethod.Patch) { verb = HttpVerb.Patch; return true; }
        if (method == HttpMethod.Delete) { verb = HttpVerb.Delete; return true; }
        if (method == HttpMethod.Head) { verb = HttpVerb.Head; return true; }
        if (method == HttpMethod.Options) { verb = HttpVerb.Options; return true; }

        verb = HttpVerb.Get;
        return false;
    }

    private static string? FirstNonEmpty(params string?[] candidates) =>
        candidates.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
}
