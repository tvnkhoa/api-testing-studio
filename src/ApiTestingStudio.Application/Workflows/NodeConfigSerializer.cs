using System.Text.Json;

namespace ApiTestingStudio.Application.Workflows;

/// <summary>
/// Serializes node <c>Config</c> payloads to and from JSON. Centralized so the engine handlers, the
/// persistence path, and the designer's property inspector stay in sync. Enums serialize as integers
/// (Web defaults), matching the database convention.
/// </summary>
public static class NodeConfigSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, Options);

    /// <summary>
    /// Serializes a weakly-typed config object by its <em>runtime</em> type. Used by the designer,
    /// which holds the typed <c>*NodeConfig</c> record as <see cref="object"/>; a plain
    /// <c>Serialize&lt;object&gt;</c> would emit an empty payload. Returns null for a null config.
    /// </summary>
    public static string? SerializeObject(object? value) =>
        value is null ? null : JsonSerializer.Serialize(value, value.GetType(), Options);

    public static T? Deserialize<T>(string? json) =>
        string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, Options);
}
