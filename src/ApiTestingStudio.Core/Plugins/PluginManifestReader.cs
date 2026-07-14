using System.IO;
using System.Text.Json;
using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Core.Plugins;

/// <summary>
/// Reads and validates a directory plugin's <c>manifest.json</c> into a <see cref="PluginManifest"/>.
/// Malformed or incomplete manifests come back as a failed <see cref="Result{T}"/> with a typed
/// reason so the caller can quarantine the plugin instead of throwing.
/// </summary>
public sealed class PluginManifestReader
{
    /// <summary>Conventional manifest file name inside each plugin folder.</summary>
    public const string ManifestFileName = "manifest.json";

    /// <summary>Error code when no manifest file exists in the plugin folder.</summary>
    public const string MissingCode = "plugin.manifest_missing";

    /// <summary>Error code when the manifest cannot be read or parsed.</summary>
    public const string InvalidCode = "plugin.manifest_invalid";

    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Reads and validates the manifest in <paramref name="pluginDirectory"/>.</summary>
    public Result<PluginManifest> ReadFromDirectory(string pluginDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginDirectory);

        var path = Path.Combine(pluginDirectory, ManifestFileName);
        if (!File.Exists(path))
        {
            return Result.Failure<PluginManifest>(new Error(
                MissingCode, $"No {ManifestFileName} found in '{pluginDirectory}'."));
        }

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (IOException ex)
        {
            return Result.Failure<PluginManifest>(new Error(InvalidCode, $"Cannot read manifest: {ex.Message}"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Result.Failure<PluginManifest>(new Error(InvalidCode, $"Cannot read manifest: {ex.Message}"));
        }

        return Parse(json, Path.GetFileName(pluginDirectory.TrimEnd(Path.DirectorySeparatorChar)));
    }

    /// <summary>Parses and validates manifest JSON. <paramref name="source"/> is used in error messages.</summary>
    public Result<PluginManifest> Parse(string json, string source)
    {
        ArgumentNullException.ThrowIfNull(json);

        PluginManifestDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<PluginManifestDto>(json, _serializerOptions);
        }
        catch (JsonException ex)
        {
            return Result.Failure<PluginManifest>(new Error(InvalidCode, $"Invalid manifest JSON in '{source}': {ex.Message}"));
        }

        if (dto is null)
        {
            return Result.Failure<PluginManifest>(new Error(InvalidCode, $"Empty manifest in '{source}'."));
        }

        if (string.IsNullOrWhiteSpace(dto.Id) ||
            string.IsNullOrWhiteSpace(dto.Name) ||
            string.IsNullOrWhiteSpace(dto.EntryAssembly))
        {
            return Result.Failure<PluginManifest>(new Error(
                InvalidCode, $"Manifest in '{source}' must set id, name, and entryAssembly."));
        }

        if (!Version.TryParse(dto.Version, out var version))
        {
            return Result.Failure<PluginManifest>(new Error(InvalidCode, $"Manifest in '{source}' has an invalid version '{dto.Version}'."));
        }

        if (!Version.TryParse(dto.MinHostApiVersion, out var minHostApiVersion))
        {
            return Result.Failure<PluginManifest>(new Error(
                InvalidCode, $"Manifest in '{source}' has an invalid minHostApiVersion '{dto.MinHostApiVersion}'."));
        }

        Version? maxHostApiVersion = null;
        if (!string.IsNullOrWhiteSpace(dto.MaxHostApiVersion))
        {
            if (!Version.TryParse(dto.MaxHostApiVersion, out var parsedMax))
            {
                return Result.Failure<PluginManifest>(new Error(
                    InvalidCode, $"Manifest in '{source}' has an invalid maxHostApiVersion '{dto.MaxHostApiVersion}'."));
            }

            maxHostApiVersion = parsedMax;
        }

        return Result.Success(new PluginManifest(
            dto.Id,
            dto.Name,
            version,
            dto.EntryAssembly,
            minHostApiVersion,
            maxHostApiVersion,
            string.IsNullOrWhiteSpace(dto.EntryType) ? null : dto.EntryType,
            string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description));
    }

    private sealed record PluginManifestDto(
        string? Id,
        string? Name,
        string? Version,
        string? EntryAssembly,
        string? MinHostApiVersion,
        string? MaxHostApiVersion,
        string? EntryType,
        string? Description);
}
