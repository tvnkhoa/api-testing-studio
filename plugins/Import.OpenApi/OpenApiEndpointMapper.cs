using System.Text.Json;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Plugin.Abstractions.Importing;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace ApiTestingStudio.Import.OpenApi;

/// <summary>
/// Maps an OpenAPI 3.x / Swagger 2.0 document (JSON or YAML) into a single <see cref="Service"/> plus
/// one <see cref="Endpoint"/> per (path, HTTP method) operation. Shared by the OpenAPI and Scalar
/// importers (Scalar reference endpoints expose an underlying OpenAPI document).
/// </summary>
public static class OpenApiEndpointMapper
{
    private static readonly JsonSerializerOptions HeaderJsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Parses raw OpenAPI text (JSON or YAML) and maps it. Throws on unparseable input.</summary>
    public static ImportResult Map(string content, string? fallbackName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var document = new OpenApiStringReader().Read(content, out var diagnostic);
        if (document is null)
        {
            throw new InvalidOperationException("The OpenAPI document could not be parsed.");
        }

        // A document with no paths and reader errors is effectively unusable — surface the reasons.
        if ((document.Paths is null || document.Paths.Count == 0) && diagnostic.Errors.Count > 0)
        {
            var reasons = string.Join("; ", diagnostic.Errors.Select(e => e.Message));
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

                foreach (var (operationType, operation) in item.Operations)
                {
                    if (!TryMapVerb(operationType, out var verb))
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
            .Select(p => new HttpHeader(p.Name, string.Empty))
            .ToList();

        return headers.Count == 0 ? null : JsonSerializer.Serialize(headers, HeaderJsonOptions);
    }

    private static bool TryMapVerb(OperationType operationType, out HttpVerb verb)
    {
        switch (operationType)
        {
            case OperationType.Get: verb = HttpVerb.Get; return true;
            case OperationType.Post: verb = HttpVerb.Post; return true;
            case OperationType.Put: verb = HttpVerb.Put; return true;
            case OperationType.Patch: verb = HttpVerb.Patch; return true;
            case OperationType.Delete: verb = HttpVerb.Delete; return true;
            case OperationType.Head: verb = HttpVerb.Head; return true;
            case OperationType.Options: verb = HttpVerb.Options; return true;
            default: verb = HttpVerb.Get; return false; // Trace has no HttpVerb equivalent.
        }
    }

    private static string? FirstNonEmpty(params string?[] candidates) =>
        candidates.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
}
