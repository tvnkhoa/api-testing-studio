using System.Text.Json;

namespace ApiTestingStudio.Application.Workflows;

/// <summary>
/// Serializes node <c>Config</c> payloads to and from JSON. Centralized so the engine handlers and
/// any persistence path stay in sync. Enums serialize as integers (Web defaults), matching the
/// database convention.
/// </summary>
internal static class NodeConfigSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, Options);

    public static T? Deserialize<T>(string? json) =>
        string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, Options);
}
