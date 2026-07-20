using System.Net.Http;
using System.Text.Json;
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
                        Path = path,
                        Description = FirstNonEmpty(operation.Summary, operation.Description),
                        SortOrder = sortOrder++,
                        DefaultHeaders = BuildHeaders(operation),
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
